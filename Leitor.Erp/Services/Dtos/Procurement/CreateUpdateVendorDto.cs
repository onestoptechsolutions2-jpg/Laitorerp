using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class CreateUpdateVendorDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(64)]
    public string? Phone { get; set; }

    [StringLength(512)]
    public string? AddressLine { get; set; }

    [StringLength(128)]
    public string? City { get; set; }

    [StringLength(128)]
    public string? State { get; set; }

    [StringLength(32)]
    public string? PostalCode { get; set; }

    [StringLength(128)]
    public string? Country { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public Guid? PortalUserId { get; set; }
    public Guid? WithholdingTaxRateId { get; set; }
}
