using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateCustomerContractDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(64)]
    public string ContractNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    public CustomerContractType Type { get; set; } = CustomerContractType.Maintenance;

    public CustomerContractStatus Status { get; set; } = CustomerContractStatus.Draft;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public decimal? Value { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
