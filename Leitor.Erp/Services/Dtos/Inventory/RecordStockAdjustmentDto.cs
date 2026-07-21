using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Inventory;

// Covers only the two manual movement types (AdjustmentIncrease/AdjustmentDecrease) - Receipt/Issue/
// Transfer are always system-generated from a GoodsReceipt or Order, never entered by hand here.
public class RecordStockAdjustmentDto
{
    [Required]
    public Guid ProductId { get; set; }

    public Guid? WarehouseId { get; set; }

    [Required]
    public DateTime MovementDate { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    public bool IsIncrease { get; set; } = true;

    [StringLength(2000)]
    public string? Notes { get; set; }
}
