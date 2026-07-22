using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Reconciliation matches a BankStatementLine (imported from a pasted CSV, see ImportStatementLinesAsync)
// one-to-one against a JournalEntryLine already posted to the bank account's linked GL account -
// no file upload/object storage plumbing, consistent with this app's lightweight-infra bias.
public class BankReconciliationAppService : ApplicationService
{
    private readonly IRepository<BankAccount, Guid> _bankAccountRepository;
    private readonly IRepository<BankStatementLine, Guid> _bankStatementLineRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;

    public BankReconciliationAppService(
        IRepository<BankAccount, Guid> bankAccountRepository,
        IRepository<BankStatementLine, Guid> bankStatementLineRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository)
    {
        _bankAccountRepository = bankAccountRepository;
        _bankStatementLineRepository = bankStatementLineRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
    }

    public async Task<int> ImportStatementLinesAsync(Guid bankAccountId, string csvText)
    {
        await CheckPolicyAsync(ErpPermissions.Banking.Create);

        var rows = csvText
            .Split('\n')
            .Select(x => x.Trim().TrimEnd('\r'))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var lines = new List<BankStatementLine>();

        foreach (var row in rows)
        {
            var fields = row.Split(',');
            if (fields.Length < 3)
            {
                continue;
            }

            if (!DateTime.TryParse(fields[0].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var transactionDate))
            {
                continue;
            }

            if (!decimal.TryParse(fields[^1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                continue;
            }

            var description = string.Join(",", fields.Skip(1).Take(fields.Length - 2)).Trim();

            lines.Add(new BankStatementLine(GuidGenerator.Create(), bankAccountId, transactionDate, description, amount));
        }

        if (lines.Count == 0)
        {
            throw new UserFriendlyException("No valid rows found. Expected one \"Date,Description,Amount\" line per row.");
        }

        await _bankStatementLineRepository.InsertManyAsync(lines, autoSave: true);
        return lines.Count;
    }

    public async Task<List<BankStatementLineDto>> GetUnreconciledStatementLinesAsync(Guid bankAccountId)
    {
        await CheckPolicyAsync(ErpPermissions.Banking.Default);

        var lines = await _bankStatementLineRepository.GetListAsync(x => x.BankAccountId == bankAccountId && !x.IsReconciled);
        return ObjectMapper.Map<List<BankStatementLine>, List<BankStatementLineDto>>(lines.OrderBy(x => x.TransactionDate).ToList());
    }

    public async Task<List<UnreconciledGlLineDto>> GetUnreconciledGlLinesAsync(Guid bankAccountId)
    {
        await CheckPolicyAsync(ErpPermissions.Banking.Default);

        var bankAccount = await _bankAccountRepository.GetAsync(bankAccountId);
        var matchedLineIds = (await _bankStatementLineRepository.GetListAsync(
            x => x.BankAccountId == bankAccountId && x.MatchedJournalEntryLineId != null))
            .Select(x => x.MatchedJournalEntryLineId!.Value)
            .ToHashSet();

        var glLines = (await _journalEntryLineRepository.GetListAsync(x => x.AccountId == bankAccount.LinkedGlAccountId))
            .Where(x => !matchedLineIds.Contains(x.Id))
            .ToList();

        var entryIds = glLines.Select(x => x.JournalEntryId).Distinct().ToList();
        var entriesById = entryIds.Count > 0
            ? (await _journalEntryRepository.GetListAsync(x => entryIds.Contains(x.Id))).ToDictionary(x => x.Id)
            : new Dictionary<Guid, JournalEntry>();

        return glLines
            .Select(x => new UnreconciledGlLineDto
            {
                JournalEntryLineId = x.Id,
                EntryDate = entriesById.GetValueOrDefault(x.JournalEntryId)?.EntryDate ?? default,
                EntryNumber = entriesById.GetValueOrDefault(x.JournalEntryId)?.EntryNumber ?? string.Empty,
                Description = entriesById.GetValueOrDefault(x.JournalEntryId)?.Description ?? string.Empty,
                Debit = x.Debit,
                Credit = x.Credit
            })
            .OrderBy(x => x.EntryDate)
            .ToList();
    }

    public async Task MatchAsync(Guid statementLineId, Guid journalEntryLineId)
    {
        await CheckPolicyAsync(ErpPermissions.Banking.Edit);

        var statementLine = await _bankStatementLineRepository.GetAsync(statementLineId);
        if (statementLine.IsReconciled)
        {
            throw new UserFriendlyException("This statement line is already reconciled.");
        }

        statementLine.IsReconciled = true;
        statementLine.MatchedJournalEntryLineId = journalEntryLineId;
        await _bankStatementLineRepository.UpdateAsync(statementLine, autoSave: true);
    }

    public async Task<BankReconciliationSummaryDto> GetSummaryAsync(Guid bankAccountId, DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Banking.Default);

        var bankAccount = await _bankAccountRepository.GetAsync(bankAccountId);

        var glLines = await _journalEntryLineRepository.GetListAsync(x => x.AccountId == bankAccount.LinkedGlAccountId);
        var entryIds = glLines.Select(x => x.JournalEntryId).Distinct().ToList();
        var entryDatesById = entryIds.Count > 0
            ? (await _journalEntryRepository.GetListAsync(x => entryIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.EntryDate)
            : new Dictionary<Guid, DateTime>();

        var glBalance = glLines
            .Where(x => entryDatesById.GetValueOrDefault(x.JournalEntryId) <= asOfDate)
            .Sum(x => (x.Debit - x.Credit) * x.ExchangeRateToBase);

        var reconciledLines = await _bankStatementLineRepository.GetListAsync(
            x => x.BankAccountId == bankAccountId && x.IsReconciled && x.TransactionDate <= asOfDate);
        var reconciledBalance = bankAccount.OpeningBalance + reconciledLines.Sum(x => x.Amount);

        return new BankReconciliationSummaryDto
        {
            GlBalance = glBalance,
            ReconciledStatementBalance = reconciledBalance,
            Difference = glBalance - reconciledBalance
        };
    }
}
