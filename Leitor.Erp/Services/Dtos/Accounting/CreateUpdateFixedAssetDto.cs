using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Accounting;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateUpdateFixedAssetDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public FixedAssetCategory Category { get; set; } = FixedAssetCategory.Equipment;
    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue)]
    public decimal PurchaseCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalvageValue { get; set; }

    [Range(1, 1200)]
    public int UsefulLifeMonths { get; set; } = 60;

    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.InUse;

    [Required]
    public Guid AssetAccountId { get; set; }

    [Required]
    public Guid DepreciationExpenseAccountId { get; set; }

    [Required]
    public Guid AccumulatedDepreciationAccountId { get; set; }
}
