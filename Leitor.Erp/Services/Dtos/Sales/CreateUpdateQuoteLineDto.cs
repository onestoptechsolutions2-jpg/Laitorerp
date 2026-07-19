using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateQuoteLineDto
{
    [Required]
    public Guid QuoteId { get; set; }

    public Guid? ProductId { get; set; }

    [Required]
    [StringLength(512)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    // Null means "use Product.TaxRateId, or the system default TaxRate if that's also null too" -
    // resolved by QuoteLineAppService.MapToEntityAsync, not here.
    public Guid? TaxRateId { get; set; }
}
