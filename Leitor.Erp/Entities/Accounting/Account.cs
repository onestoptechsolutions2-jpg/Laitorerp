using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// A Chart of Accounts entry. Balances are never stored here - always summed from
// JournalEntryLines by GeneralLedgerReportAppService, same "compute, never store" discipline as
// InvoicePaymentStatus.
public class Account : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public SystemAccountRole SystemRole { get; set; } = SystemAccountRole.None;
    public bool IsActive { get; set; } = true;

    protected Account()
    {
    }

    public Account(Guid id, string code, string name, AccountType type)
        : base(id)
    {
        Code = code;
        Name = name;
        Type = type;
    }
}
