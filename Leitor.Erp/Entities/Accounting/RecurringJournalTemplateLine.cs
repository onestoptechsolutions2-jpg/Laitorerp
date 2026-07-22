using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// Mirrors JournalEntryLine's shape exactly (one Debit or Credit per line) but as a reusable
// template line rather than a posted line - RecurringJournalWorker copies these onto a real
// JournalEntry/JournalEntryLine each time the template runs.
public class RecurringJournalTemplateLine : FullAuditedAggregateRoot<Guid>
{
    public Guid RecurringJournalTemplateId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    protected RecurringJournalTemplateLine()
    {
    }

    public RecurringJournalTemplateLine(Guid id, Guid recurringJournalTemplateId, Guid accountId)
        : base(id)
    {
        RecurringJournalTemplateId = recurringJournalTemplateId;
        AccountId = accountId;
    }
}
