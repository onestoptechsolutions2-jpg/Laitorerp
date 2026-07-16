using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateCustomerContactDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(128)]
    public string? JobTitle { get; set; }

    [StringLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    public bool IsPrimary { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
