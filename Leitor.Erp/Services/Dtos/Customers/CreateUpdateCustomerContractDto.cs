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

    // Per-priority SLA response targets in hours - leave blank to inherit the app-wide default
    // table (see TicketAppService.ResolveSlaWindowAsync).
    [Range(1, 8760)]
    public int? SlaUrgentHours { get; set; }

    [Range(1, 8760)]
    public int? SlaHighHours { get; set; }

    [Range(1, 8760)]
    public int? SlaMediumHours { get; set; }

    [Range(1, 8760)]
    public int? SlaLowHours { get; set; }
}
