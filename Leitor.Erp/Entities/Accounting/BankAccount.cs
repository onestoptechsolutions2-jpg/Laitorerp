using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// Wraps a GL Account (LinkedGlAccountId, typically Type=Asset) with the extra metadata a bank
// reconciliation needs (account number, bank name, opening balance) that doesn't belong on the
// Chart of Accounts entry itself. Reconciliation matches BankStatementLines against
// JournalEntryLines posted to LinkedGlAccountId - see Services/Accounting/BankReconciliationAppService.cs.
public class BankAccount : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid LinkedGlAccountId { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime OpeningBalanceDate { get; set; }

    protected BankAccount()
    {
    }

    public BankAccount(Guid id, string name, Guid linkedGlAccountId)
        : base(id)
    {
        Name = name;
        LinkedGlAccountId = linkedGlAccountId;
    }
}
