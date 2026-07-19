using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateOrderPaymentMilestoneDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [StringLength(256)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100)]
    public decimal Percent { get; set; }
}
