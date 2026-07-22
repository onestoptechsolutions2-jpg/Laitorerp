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
    public static (List<AccountNet> RevenueLines, List<AccountNet> ExpenseLines) ComputeAccountNets(
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
            if (accountLines.Count == 0)
            {
                continue;
            }

            var debitTotal = accountLines.Sum(x => x.Debit * x.ExchangeRateToBase);
            var creditTotal = accountLines.Sum(x => x.Credit * x.ExchangeRateToBase);

            if (account.Type == AccountType.Revenue)
            {
                var amount = creditTotal - debitTotal;
                if (amount != 0)
                {
                    revenueLines.Add(new AccountNet(account, amount));
                }
            }
            else
            {
                var amount = debitTotal - creditTotal;
                if (amount != 0)
                {
                    expenseLines.Add(new AccountNet(account, amount));
                }
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
