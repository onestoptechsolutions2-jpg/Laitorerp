using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// A reusable journal entry shape (rent, subscriptions, etc.) - RecurringJournalWorker posts a real
// JournalEntry from this template's RecurringJournalTemplateLines whenever NextRunDate arrives,
// then advances NextRunDate by Frequency. Posting goes through the normal
// JournalEntryAppService.CreateAsync path (not JournalPostingService), so the balance check and
// FiscalPeriodGuard both apply automatically, same as any other manual entry.
public class RecurringJournalTemplate : FullAuditedAggregateRoot<Guid>
{
    public string Description { get; set; } = string.Empty;
    public RecurringJournalFrequency Frequency { get; set; } = RecurringJournalFrequency.Monthly;
    public DateTime NextRunDate { get; set; }
    public bool IsActive { get; set; } = true;

    protected RecurringJournalTemplate()
    {
    }

    public RecurringJournalTemplate(Guid id, string description, DateTime nextRunDate)
        : base(id)
    {
        Description = description;
        NextRunDate = nextRunDate;
    }
}
