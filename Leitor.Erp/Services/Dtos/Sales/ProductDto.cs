using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class ProductDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public ProductType Type { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsBundle { get; set; }
    public bool TrackInventory { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }

    // Resolved by ProductAppService - not a stored column, Mapperly won't map it.
    public string? CategoryName { get; set; }
    public decimal? QuantityOnHand { get; set; }
}
