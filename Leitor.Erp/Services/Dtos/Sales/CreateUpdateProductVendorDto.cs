using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdateProductVendorDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid VendorId { get; set; }

    [StringLength(64)]
    public string? VendorSku { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    [Range(0, 3650)]
    public int? LeadTimeDays { get; set; }

    public bool IsPreferred { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
