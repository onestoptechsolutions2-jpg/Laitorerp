using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateProductDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Sku { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProductType Type { get; set; } = ProductType.Product;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    public Guid? TaxRateId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsBundle { get; set; }

    public bool TrackInventory { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? ReorderPoint { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? ReorderQuantity { get; set; }
}
