using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Accounting;

namespace Leitor.Erp.Services.Accounting;

// Centralizes the revenue/expense account-netting loop previously duplicated verbatim in
// GeneralLedgerReportAppService.ComputeNetIncomeByAccountAsync and
// ProjectReportAppService.GetProjectPnLAsync - both grouped JournalEntryLines by account and
// applied the same Credit-Debit (Revenue) / Debit-Credit (Expense) sign convention, differing
// only in which lines were passed in (a date range vs. a ProjectId filter).
public static class LedgerMath
{
    // Skips accounts with no activity in the given lines - the right behavior for Income
    // Statement/Project P&L, which shouldn't list every unused account in the Chart of Accounts.
    public static (List<AccountNet> RevenueLines, List<AccountNet> ExpenseLines) ComputeAccountNets(
        IEnumerable<JournalEntryLine> lines, IEnumerable<Account> accounts)
    {
        var (revenueLines, expenseLines) = ComputeAllAccountNets(lines, accounts);
        return (
            revenueLines.Where(x => x.Amount != 0).ToList(),
            expenseLines.Where(x => x.Amount != 0).ToList()
        );
    }

    // Unfiltered variant for callers that need every Revenue/Expense account represented even at
    // zero (e.g. BudgetVarianceReportAppService, where an account with a budget but no actual
    // spend yet still needs to show up as a variance).
    public static (List<AccountNet> RevenueLines, List<AccountNet> ExpenseLines) ComputeAllAccountNets(
        IEnumerable<JournalEntryLine> lines, IEnumerable<Account> accounts)
    {
        var linesByAccountId = lines.ToLookup(x => x.AccountId);

        var revenueLines = new List<AccountNet>();
        var expenseLines = new List<AccountNet>();

        foreach (var account in accounts)
        {
            if (account.Type is not (AccountType.Revenue or AccountType.Expense))
            {
                continue;
            }

            var accountLines = linesByAccountId[account.Id].ToList();
            var debitTotal = accountLines.Sum(x => x.Debit * x.ExchangeRateToBase);
            var creditTotal = accountLines.Sum(x => x.Credit * x.ExchangeRateToBase);

            if (account.Type == AccountType.Revenue)
            {
                revenueLines.Add(new AccountNet(account, creditTotal - debitTotal));
            }
            else
            {
                expenseLines.Add(new AccountNet(account, debitTotal - creditTotal));
            }
        }

        return (revenueLines.OrderBy(x => x.Account.Code).ToList(), expenseLines.OrderBy(x => x.Account.Code).ToList());
    }

    public readonly struct AccountNet
    {
        public AccountNet(Account account, decimal amount)
        {
            Account = account;
            Amount = amount;
        }

        public Account Account { get; }
        public decimal Amount { get; }
    }
}
