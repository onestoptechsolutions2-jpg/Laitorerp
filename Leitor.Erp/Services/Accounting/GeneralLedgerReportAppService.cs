using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// SalesAnalyticsAppService/DashboardAppService. Balances are never stored anywhere - always
// summed from JournalEntryLines here, on demand, same "compute, never store" discipline as
// InvoicePaymentStatus.
public class GeneralLedgerReportAppService : ApplicationService
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;

    public GeneralLedgerReportAppService(
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var (linesByAccountId, accountsById) = await LoadLinesUpToAsync(asOfDate);

        var result = new TrialBalanceDto { AsOfDate = asOfDate };

        foreach (var group in linesByAccountId)
        {
            var debitTotal = group.Sum(x => x.Debit * x.ExchangeRateToBase);
            var creditTotal = group.Sum(x => x.Credit * x.ExchangeRateToBase);
            if (debitTotal == 0 && creditTotal == 0)
            {
                continue;
            }

            var account = accountsById.GetValueOrDefault(group.Key);
            result.Lines.Add(new TrialBalanceLineDto
            {
                AccountCode = account?.Code ?? "?",
                AccountName = account?.Name ?? "Unknown account",
                DebitTotal = debitTotal,
                CreditTotal = creditTotal
            });
        }

        result.Lines = result.Lines.OrderBy(x => x.AccountCode).ToList();
        result.TotalDebit = result.Lines.Sum(x => x.DebitTotal);
        result.TotalCredit = result.Lines.Sum(x => x.CreditTotal);
        result.IsBalanced = Math.Round(result.TotalDebit - result.TotalCredit, 2) == 0;

        return result;
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime fromDate, DateTime toDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var netByAccount = await ComputeNetIncomeByAccountAsync(fromDate, toDate);

        var result = new IncomeStatementDto { FromDate = fromDate, ToDate = toDate };
        result.RevenueLines = netByAccount.revenueLines;
        result.ExpenseLines = netByAccount.expenseLines;
        result.TotalRevenue = result.RevenueLines.Sum(x => x.Amount);
        result.TotalExpense = result.ExpenseLines.Sum(x => x.Amount);
        result.NetIncome = result.TotalRevenue - result.TotalExpense;

        return result;
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var (linesByAccountId, accountsById) = await LoadLinesUpToAsync(asOfDate);

        var result = new BalanceSheetDto { AsOfDate = asOfDate };

        foreach (var group in linesByAccountId)
        {
            if (!accountsById.TryGetValue(group.Key, out var account))
            {
                continue;
            }

            var debitTotal = group.Sum(x => x.Debit * x.ExchangeRateToBase);
            var creditTotal = group.Sum(x => x.Credit * x.ExchangeRateToBase);

            switch (account.Type)
            {
                case AccountType.Asset:
                    result.AssetLines.Add(new BalanceSheetLineDto { AccountCode = account.Code, AccountName = account.Name, Amount = debitTotal - creditTotal });
                    break;
                case AccountType.Liability:
                    result.LiabilityLines.Add(new BalanceSheetLineDto { AccountCode = account.Code, AccountName = account.Name, Amount = creditTotal - debitTotal });
                    break;
                case AccountType.Equity:
                    result.EquityLines.Add(new BalanceSheetLineDto { AccountCode = account.Code, AccountName = account.Name, Amount = creditTotal - debitTotal });
                    break;
            }
        }

        result.AssetLines = result.AssetLines.Where(x => x.Amount != 0).OrderBy(x => x.AccountCode).ToList();
        result.LiabilityLines = result.LiabilityLines.Where(x => x.Amount != 0).OrderBy(x => x.AccountCode).ToList();
        result.EquityLines = result.EquityLines.Where(x => x.Amount != 0).OrderBy(x => x.AccountCode).ToList();
        result.TotalAssets = result.AssetLines.Sum(x => x.Amount);
        result.TotalLiabilities = result.LiabilityLines.Sum(x => x.Amount);
        result.TotalEquity = result.EquityLines.Sum(x => x.Amount);

        var netIncomeToDate = await ComputeNetIncomeByAccountAsync(DateTime.MinValue, asOfDate);
        result.RetainedEarnings = netIncomeToDate.revenueLines.Sum(x => x.Amount) - netIncomeToDate.expenseLines.Sum(x => x.Amount);

        return result;
    }

    private async Task<(List<IncomeStatementLineDto> revenueLines, List<IncomeStatementLineDto> expenseLines)> ComputeNetIncomeByAccountAsync(
        DateTime fromDate, DateTime toDate)
    {
        var entries = await _journalEntryRepository.GetListAsync(x => x.EntryDate >= fromDate && x.EntryDate <= toDate);
        var entryIds = entries.Select(x => x.Id).ToList();
        var lines = entryIds.Count > 0
            ? await _journalEntryLineRepository.GetListAsync(x => entryIds.Contains(x.JournalEntryId))
            : new List<JournalEntryLine>();

        var accounts = await _accountRepository.GetListAsync();
        var (revenueNets, expenseNets) = LedgerMath.ComputeAccountNets(lines, accounts);

        return (
            revenueNets.Select(x => new IncomeStatementLineDto { AccountCode = x.Account.Code, AccountName = x.Account.Name, Amount = x.Amount }).ToList(),
            expenseNets.Select(x => new IncomeStatementLineDto { AccountCode = x.Account.Code, AccountName = x.Account.Name, Amount = x.Amount }).ToList()
        );
    }

    private async Task<(ILookup<Guid, JournalEntryLine> linesByAccountId, Dictionary<Guid, Account> accountsById)> LoadLinesUpToAsync(DateTime asOfDate)
    {
        var entries = await _journalEntryRepository.GetListAsync(x => x.EntryDate <= asOfDate);
        var entryIds = entries.Select(x => x.Id).ToList();
        var lines = entryIds.Count > 0
            ? await _journalEntryLineRepository.GetListAsync(x => entryIds.Contains(x.JournalEntryId))
            : new List<JournalEntryLine>();

        var accounts = await _accountRepository.GetListAsync();
        var accountsById = accounts.ToDictionary(x => x.Id);

        return (lines.ToLookup(x => x.AccountId), accountsById);
    }
}
