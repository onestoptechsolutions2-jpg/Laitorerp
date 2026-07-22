using System;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class FixedAssetDto : FullAuditedEntityDto<Guid>
{
    public string AssetNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FixedAssetCategory Category { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal SalvageValue { get; set; }
    public int UsefulLifeMonths { get; set; }
    public FixedAssetStatus Status { get; set; }
    public Guid AssetAccountId { get; set; }
    public Guid DepreciationExpenseAccountId { get; set; }
    public Guid AccumulatedDepreciationAccountId { get; set; }

    // Resolved by FixedAssetAppService from the Account repository - not stored columns.
    public string? AssetAccountName { get; set; }
    public string? DepreciationExpenseAccountName { get; set; }
    public string? AccumulatedDepreciationAccountName { get; set; }

    // Computed from DepreciationEntry rows, never stored - same "compute, never store"
    // discipline as every other derived total in this app.
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
    public bool IsFullyDepreciated { get; set; }
}
