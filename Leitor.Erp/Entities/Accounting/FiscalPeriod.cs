using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// One row per calendar month, created on demand the first time it's locked (see
// Services/Accounting/FiscalPeriodGuard.cs) - a month with no row here is implicitly unlocked.
// "Year-End Close" in this app means locking all 12 months of a year at once, not a traditional
// GL-zeroing closing entry - see the design note in the accounting-module plan for why (Balance
// Sheet already computes Retained Earnings live since inception, so a closing entry would
// conflict with that).
public class FiscalPeriod : FullAuditedAggregateRoot<Guid>
{
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedDate { get; set; }

    protected FiscalPeriod()
    {
    }

    public FiscalPeriod(Guid id, int year, int month)
        : base(id)
    {
        Year = year;
        Month = month;
    }
}
