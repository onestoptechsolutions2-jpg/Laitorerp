using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdatePriceListItemDto
{
    [Required]
    public Guid PriceListId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
