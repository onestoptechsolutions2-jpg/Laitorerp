using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdatePriceListItemDto
{
    [Required]
    public Guid PriceListId { get; set; }

    // Exactly one of these two must be set - validated in PriceListItemAppService (not a
    // DataAnnotation, since "exactly one of two optional fields" isn't expressible with the
    // built-in attributes without a custom one).
    public Guid? ProductId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public RateType RateType { get; set; } = RateType.Fixed;
}
