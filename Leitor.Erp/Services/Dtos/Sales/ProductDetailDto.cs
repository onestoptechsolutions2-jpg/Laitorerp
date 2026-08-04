using System;

namespace Leitor.Erp.Services.Dtos.Sales;

/// <summary>
/// DTO for product details fetched during quote/proposal line item creation.
/// Includes pricing from selected price list if applicable.
/// </summary>
public class ProductDetailDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Cost { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxRatePercent { get; set; }
}
