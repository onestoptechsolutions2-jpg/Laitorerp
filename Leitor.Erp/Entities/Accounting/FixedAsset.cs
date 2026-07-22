using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// The asset register. PurchaseCost/SalvageValue/UsefulLifeMonths drive straight-line
// depreciation (see Services/Accounting/DepreciationAppService.cs) - the acquisition itself is
// NOT auto-posted to the GL from here, since the purchase was likely already recorded via
// Procurement (SupplierInvoice) or a manual JournalEntry; re-posting it here would double-count.
// AssetAccountId/DepreciationExpenseAccountId/AccumulatedDepreciationAccountId are picked per
// asset (not a single global SystemAccountRole) since different asset categories often book to
// different accounts.
public class FixedAsset : FullAuditedAggregateRoot<Guid>
{
    public string AssetNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FixedAssetCategory Category { get; set; } = FixedAssetCategory.Equipment;
    public DateTime PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal SalvageValue { get; set; }
    public int UsefulLifeMonths { get; set; } = 60;
    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.InUse;

    public Guid AssetAccountId { get; set; }
    public Guid DepreciationExpenseAccountId { get; set; }
    public Guid AccumulatedDepreciationAccountId { get; set; }

    protected FixedAsset()
    {
    }

    public FixedAsset(Guid id, string assetNumber, string name)
        : base(id)
    {
        AssetNumber = assetNumber;
        Name = name;
    }
}
