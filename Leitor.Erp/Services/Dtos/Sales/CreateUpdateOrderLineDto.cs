using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateOrderLineDto
{
    [Required]
    public Guid OrderId { get; set; }

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
}
