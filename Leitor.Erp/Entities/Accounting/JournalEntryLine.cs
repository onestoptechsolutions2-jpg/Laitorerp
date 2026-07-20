using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// Exactly one of Debit/Credit is non-zero per line - enforced by JournalEntryAppService, not the
// entity itself. CurrencyCode/ExchangeRateToBase are snapshotted at the parent entry's EntryDate,
// same discipline as every other money-carrying entity in this app.
public class JournalEntryLine : FullAuditedAggregateRoot<Guid>
{
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateToBase { get; set; } = 1m;

    protected JournalEntryLine()
    {
    }

    public JournalEntryLine(Guid id, Guid journalEntryId, Guid accountId)
        : base(id)
    {
        JournalEntryId = journalEntryId;
        AccountId = accountId;
    }
}
