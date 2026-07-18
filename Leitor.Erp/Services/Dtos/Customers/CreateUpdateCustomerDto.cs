using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateCustomerDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

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

    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public Guid? AccountOwnerUserId { get; set; }

    public Guid? PortalUserId { get; set; }
}
