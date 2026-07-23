using System;

namespace Leitor.Erp.Services.Dtos.Pos;

public class ProductSearchResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal UnitPrice { get; set; }
    public bool TrackInventory { get; set; }
    public decimal? StockOnHand { get; set; }
}
