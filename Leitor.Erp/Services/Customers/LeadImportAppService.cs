using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Customers;

// Extract-transform-load for the .xlsx field-ticket exports sales agents produce: every row is a
// prospect (per the business call), so there's no filtering step - just column mapping, agent
// master resolution, and dedup. Originally went through LeadAppService.CreateAsync per row, but on
// a real multi-thousand-row export that meant one AppService call (full interceptor stack) + one
// duplicate-phone query + one SaveChanges per row, which took 90+ seconds in production and got
// killed by the reverse proxy (499) before finishing. Now dedup is batched up front (one query for
// existing tickets, one for existing phones) and inserts are batched via InsertManyAsync, so the
// whole file is a handful of round-trips instead of thousands. Phone normalization still reuses
// LeadAppService.NormalizePhone (internal) rather than duplicating that logic.
public class LeadImportAppService : ErpAppService
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
    private const int InsertBatchSize = 500;

    private static readonly string[] RequiredColumns = { "Customer", "Mobile phone" };

    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;

    public LeadImportAppService(
        IRepository<Lead, Guid> leadRepository,
        IRepository<Agent, Guid> agentRepository)
    {
        _leadRepository = leadRepository;
        _agentRepository = agentRepository;
    }

    public async Task<LeadImportResultDto> ImportAsync(byte[] fileContent)
    {
        await CheckPolicyAsync(ErpPermissions.Leads.Import);

        if (fileContent.Length > MaxFileSizeBytes)
        {
            throw new UserFriendlyException($"File is too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        using var stream = new MemoryStream(fileContent);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var rows = worksheet.RowsUsed().ToList();
        if (rows.Count == 0)
        {
            throw new UserFriendlyException("The file has no data.");
        }

        var columnMap = BuildColumnMap(rows[0]);
        var missingColumns = RequiredColumns.Where(c => !columnMap.ContainsKey(c)).ToList();
        if (missingColumns.Count > 0)
        {
            throw new UserFriendlyException($"The file is missing required column(s): {string.Join(", ", missingColumns)}.");
        }

        var dataRows = rows.Skip(1).ToList();
        var result = new LeadImportResultDto { TotalRows = dataRows.Count };

        var ticketNumbersInFile = dataRows
            .Select(r => GetValue(r, columnMap, "Number"))
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .Distinct()
            .ToList();

        var alreadyImportedTickets = ticketNumbersInFile.Count > 0
            ? (await _leadRepository.GetListAsync(x => x.ExternalTicketNumber != null && ticketNumbersInFile.Contains(x.ExternalTicketNumber)))
                .Select(x => x.ExternalTicketNumber!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var agentsByName = (await _agentRepository.GetListAsync())
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingPhones = (await _leadRepository.GetListAsync(x => x.NormalizedPhone != null))
            .Select(x => x.NormalizedPhone!)
            .ToHashSet(StringComparer.Ordinal);

        var seenTicketsThisRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingInserts = new List<Lead>(InsertBatchSize);

        foreach (var row in dataRows)
        {
            var customerName = GetValue(row, columnMap, "Customer");
            var phone = GetValue(row, columnMap, "Mobile phone");

            if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(phone))
            {
                result.SkippedInvalidRow++;
                if (result.RowErrors.Count < 50)
                {
                    result.RowErrors.Add($"Row {row.RowNumber()}: missing Customer or Mobile phone, skipped.");
                }
                continue;
            }

            var ticketNumber = GetValue(row, columnMap, "Number");
            if (!string.IsNullOrEmpty(ticketNumber) &&
                (alreadyImportedTickets.Contains(ticketNumber) || !seenTicketsThisRun.Add(ticketNumber)))
            {
                result.SkippedDuplicateTicket++;
                continue;
            }

            var normalizedPhone = LeadAppService.NormalizePhone(phone);
            if (normalizedPhone != null && !existingPhones.Add(normalizedPhone))
            {
                result.SkippedDuplicatePhone++;
                continue;
            }

            var territory = GetValue(row, columnMap, "Territories");
            var agentName = GetValue(row, columnMap, "Assigned to") ?? GetValue(row, columnMap, "Updated by");
            Guid? referrerAgentId = null;
            if (!string.IsNullOrEmpty(agentName))
            {
                if (!agentsByName.TryGetValue(agentName, out var agent))
                {
                    agent = new Agent(GuidGenerator.Create(), agentName) { Territory = territory };
                    await _agentRepository.InsertAsync(agent, autoSave: true);
                    agentsByName[agentName] = agent;
                    result.NewAgentsCreated++;
                }

                referrerAgentId = agent.Id;
            }

            var lead = new Lead(GuidGenerator.Create(), customerName)
            {
                Phone = phone,
                NormalizedPhone = normalizedPhone,
                Source = LeadSource.Import,
                ReferrerAgentId = referrerAgentId,
                Territory = territory,
                Cluster = GetValue(row, columnMap, "Cluster"),
                Location = GetValue(row, columnMap, "Location"),
                Estate = GetValue(row, columnMap, "Estate"),
                ExternalAccountNumber = GetValue(row, columnMap, "Account Number (CI)"),
                ExternalTicketNumber = ticketNumber,
                Notes = BuildNote(ticketNumber, GetValue(row, columnMap, "Service"), GetValue(row, columnMap, "Issue type"))
            };

            pendingInserts.Add(lead);
            result.ImportedCount++;

            if (pendingInserts.Count >= InsertBatchSize)
            {
                await _leadRepository.InsertManyAsync(pendingInserts, autoSave: true);
                pendingInserts.Clear();
            }
        }

        if (pendingInserts.Count > 0)
        {
            await _leadRepository.InsertManyAsync(pendingInserts, autoSave: true);
        }

        return result;
    }

    private static string BuildNote(string? ticketNumber, string? service, string? issueType)
    {
        var ticketLabel = ticketNumber ?? "unknown";
        var serviceLabel = string.Join(" / ", new[] { service, issueType }.Where(s => !string.IsNullOrEmpty(s)));
        return string.IsNullOrEmpty(serviceLabel)
            ? $"Imported from field ticket {ticketLabel}"
            : $"Imported from field ticket {ticketLabel}: {serviceLabel}";
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var name = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
            {
                map[name] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    private static string? GetValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var columnNumber))
        {
            return null;
        }

        var value = row.Cell(columnNumber).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
