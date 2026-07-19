using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateProductBundleItemDto
{
    [Required]
    public Guid BundleProductId { get; set; }

    [Required]
    public Guid ComponentProductId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;
}
