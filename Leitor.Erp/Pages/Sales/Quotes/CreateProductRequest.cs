namespace Leitor.Erp.Pages.Sales.Quotes;

/// <summary>
/// Request DTO for creating a new product from the Quote Detail page modal.
/// Used by OnPostCreateProductAsync handler for JSON [FromBody] binding.
/// </summary>
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Cost { get; set; }
    public string? TaxRateId { get; set; }
}
