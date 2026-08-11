using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Services.Customers;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers LeadImportAppService, the ETL that turns a field-ticket .xlsx export from sales agents
// into Lead rows (see conversation 2026-08-11: every row in the export is a prospect - there's no
// filtering step, just column mapping + agent-master resolution + dedup).
public class LeadImportTests : ErpTestBase
{
    private static readonly string[] Headers =
    {
        "Number", "Assigned to", "Updated by", "Customer", "Mobile phone", "Service",
        "Location", "Cluster", "Territories", "Issue type", "Account Number (CI)", "Estate"
    };

    [Fact]
    public async Task ImportAsync_Creates_Leads_With_Mapped_Fields_And_Resolved_Agent()
    {
        await EnsureDatabaseCreatedAsync();

        var leadImportAppService = GetRequiredService<LeadImportAppService>();
        var leadRepository = GetRequiredService<IRepository<Lead, Guid>>();
        var agentRepository = GetRequiredService<IRepository<Agent, Guid>>();

        var file = BuildWorkbook(new[]
        {
            new[] { "WOT001", "Jane Agent", "", "Joyce Auma", "0714374936", "New Connection", "GW_Bungoma_Sirandula-33", "BUNGOMA LOWER CBD", "Bungoma", "Connection", "31073035", "GW_Bungoma_Sirandula" }
        });

        var result = await leadImportAppService.ImportAsync(file);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.NewAgentsCreated);

        var lead = (await leadRepository.GetListAsync()).Single();
        Assert.Equal("Joyce Auma", lead.Name);
        Assert.Equal(LeadSource.Import, lead.Source);
        Assert.Equal("Bungoma", lead.Territory);
        Assert.Equal("BUNGOMA LOWER CBD", lead.Cluster);
        Assert.Equal("31073035", lead.ExternalAccountNumber);
        Assert.Equal("WOT001", lead.ExternalTicketNumber);
        Assert.NotNull(lead.ReferrerAgentId);

        var agent = (await agentRepository.GetListAsync()).Single();
        Assert.Equal("Jane Agent", agent.Name);
        Assert.Equal(lead.ReferrerAgentId, agent.Id);
    }

    [Fact]
    public async Task ImportAsync_Reusing_An_Existing_Agent_Name_Does_Not_Create_A_Duplicate()
    {
        await EnsureDatabaseCreatedAsync();

        var leadImportAppService = GetRequiredService<LeadImportAppService>();
        var agentRepository = GetRequiredService<IRepository<Agent, Guid>>();

        var existingAgent = new Agent(Guid.NewGuid(), "Jane Agent");
        await agentRepository.InsertAsync(existingAgent, autoSave: true);

        var file = BuildWorkbook(new[]
        {
            new[] { "WOT001", "Jane Agent", "", "Joyce Auma", "0714374936", "New Connection", "", "", "Bungoma", "Connection", "", "" }
        });

        var result = await leadImportAppService.ImportAsync(file);

        Assert.Equal(0, result.NewAgentsCreated);
        Assert.Single(await agentRepository.GetListAsync());
    }

    [Fact]
    public async Task ImportAsync_Skips_A_Ticket_Number_Already_Imported()
    {
        await EnsureDatabaseCreatedAsync();

        var leadImportAppService = GetRequiredService<LeadImportAppService>();

        var file = BuildWorkbook(new[]
        {
            new[] { "WOT001", "Jane Agent", "", "Joyce Auma", "0714374936", "New Connection", "", "", "Bungoma", "Connection", "", "" }
        });

        var first = await leadImportAppService.ImportAsync(file);
        Assert.Equal(1, first.ImportedCount);

        var second = await leadImportAppService.ImportAsync(file);
        Assert.Equal(0, second.ImportedCount);
        Assert.Equal(1, second.SkippedDuplicateTicket);
    }

    [Fact]
    public async Task ImportAsync_Skips_A_Duplicate_Phone_Without_Aborting_The_Batch()
    {
        await EnsureDatabaseCreatedAsync();

        var leadImportAppService = GetRequiredService<LeadImportAppService>();

        var file = BuildWorkbook(new[]
        {
            new[] { "WOT001", "Jane Agent", "", "Joyce Auma", "0714374936", "New Connection", "", "", "Bungoma", "Connection", "", "" },
            new[] { "WOT002", "Jane Agent", "", "Different Name Same Phone", "0714374936", "New Connection", "", "", "Bungoma", "Connection", "", "" },
            new[] { "WOT003", "Jane Agent", "", "Another Customer", "0722000000", "New Connection", "", "", "Bungoma", "Connection", "", "" }
        });

        var result = await leadImportAppService.ImportAsync(file);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.SkippedDuplicatePhone);
    }

    [Fact]
    public async Task ImportAsync_Skips_A_Row_Missing_Customer_Or_Phone()
    {
        await EnsureDatabaseCreatedAsync();

        var leadImportAppService = GetRequiredService<LeadImportAppService>();

        var file = BuildWorkbook(new[]
        {
            new[] { "WOT001", "Jane Agent", "", "", "0714374936", "New Connection", "", "", "Bungoma", "Connection", "", "" },
            new[] { "WOT002", "Jane Agent", "", "Valid Customer", "", "New Connection", "", "", "Bungoma", "Connection", "", "" }
        });

        var result = await leadImportAppService.ImportAsync(file);

        Assert.Equal(2, result.SkippedInvalidRow);
        Assert.Equal(0, result.ImportedCount);
    }

    [Fact]
    public async Task ImportAsync_Throws_When_Required_Columns_Are_Missing()
    {
        await EnsureDatabaseCreatedAsync();

        var leadImportAppService = GetRequiredService<LeadImportAppService>();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        worksheet.Cell(1, 1).Value = "Number";
        worksheet.Cell(1, 2).Value = "Territories";
        worksheet.Cell(2, 1).Value = "WOT001";
        worksheet.Cell(2, 2).Value = "Bungoma";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        await Assert.ThrowsAsync<UserFriendlyException>(() => leadImportAppService.ImportAsync(stream.ToArray()));
    }

    private static byte[] BuildWorkbook(string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        for (var col = 0; col < Headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = Headers[col];
        }

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            for (var col = 0; col < Headers.Length; col++)
            {
                worksheet.Cell(rowIndex + 2, col + 1).Value = rows[rowIndex][col];
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
