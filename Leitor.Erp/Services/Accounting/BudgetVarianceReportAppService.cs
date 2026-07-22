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

// Read-only aggregation, plain ApplicationService - same convention as GeneralLedgerReportAppService,
// whose LedgerMath.ComputeAccountNets helper this reuses for the "actual" side.
public class BudgetVarianceReportAppService : ApplicationService
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<Budget, Guid> _budgetRepository;

    public BudgetVarianceReportAppService(
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<Budget, Guid> budgetRepository)
    {
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _budgetRepository = budgetRepository;
    }

    public async Task<BudgetVarianceReportDto> GetVarianceAsync(int fiscalYear, int? month)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var fromDate = month.HasValue ? new DateTime(fiscalYear, month.Value, 1) : new DateTime(fiscalYear, 1, 1);
        var toDate = month.HasValue ? fromDate.AddMonths(1).AddDays(-1) : new DateTime(fiscalYear, 12, 31);

        var entries = await _journalEntryRepository.GetListAsync(x => x.EntryDate >= fromDate && x.EntryDate <= toDate);
        var entryIds = entries.Select(x => x.Id).ToList();
        var lines = entryIds.Count > 0
            ? await _journalEntryLineRepository.GetListAsync(x => entryIds.Contains(x.JournalEntryId))
            : new List<JournalEntryLine>();

        var accounts = await _accountRepository.GetListAsync();
        var (revenueNets, expenseNets) = LedgerMath.ComputeAllAccountNets(lines, accounts);

        var budgets = await _budgetRepository.GetListAsync(x =>
            x.FiscalYear == fiscalYear && (!month.HasValue || x.Month == month.Value));
        var budgetByAccountId = budgets.GroupBy(x => x.AccountId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        // Skip rows with neither actual activity nor a budget - an unused account shouldn't
        // clutter the variance report, but one with either should always show up.
        revenueNets = revenueNets.Where(x => x.Amount != 0 || budgetByAccountId.ContainsKey(x.Account.Id)).ToList();
        expenseNets = expenseNets.Where(x => x.Amount != 0 || budgetByAccountId.ContainsKey(x.Account.Id)).ToList();

        return new BudgetVarianceReportDto
        {
            FiscalYear = fiscalYear,
            Month = month,
            RevenueLines = BuildLines(revenueNets, budgetByAccountId),
            ExpenseLines = BuildLines(expenseNets, budgetByAccountId)
        };
    }

    private static List<BudgetVarianceLineDto> BuildLines(List<LedgerMath.AccountNet> nets, Dictionary<Guid, decimal> budgetByAccountId)
    {
        return nets.Select(x =>
        {
            var budget = budgetByAccountId.GetValueOrDefault(x.Account.Id);
            var variance = x.Amount - budget;
            return new BudgetVarianceLineDto
            {
                AccountCode = x.Account.Code,
                AccountName = x.Account.Name,
                Actual = x.Amount,
                Budget = budget,
                Variance = variance,
                VariancePercent = budget != 0 ? Math.Round(variance / budget * 100, 1) : null
            };
        }).ToList();
    }
}
