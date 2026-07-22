using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// Append-only depreciation ledger, same "never mutate, always append" convention as
// Entities/Inventory/StockMovement.cs - one row per asset per period it was depreciated for, so
// DepreciationAppService can tell which periods are already done (and skip them) and so a
// depreciation schedule can be displayed without recomputing anything.
public class DepreciationEntry : FullAuditedAggregateRoot<Guid>
{
    public Guid FixedAssetId { get; set; }

    // Always the first of the month - the granularity depreciation is run at.
    public DateTime PeriodDate { get; set; }

    public decimal Amount { get; set; }
    public Guid JournalEntryId { get; set; }

    protected DepreciationEntry()
    {
    }

    public DepreciationEntry(Guid id, Guid fixedAssetId, DateTime periodDate, decimal amount, Guid journalEntryId)
        : base(id)
    {
        FixedAssetId = fixedAssetId;
        PeriodDate = periodDate;
        Amount = amount;
        JournalEntryId = journalEntryId;
    }
}
