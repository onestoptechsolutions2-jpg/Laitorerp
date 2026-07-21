using System;

namespace Leitor.Erp.Services.Dtos.Inventory;

public class StockOnHandLineDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal Cost { get; set; }
    public decimal Value { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }
}
