namespace Leitor.Erp.Pages.Sales.Orders;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Cost { get; set; }
    public string? TaxRateId { get; set; }
}
