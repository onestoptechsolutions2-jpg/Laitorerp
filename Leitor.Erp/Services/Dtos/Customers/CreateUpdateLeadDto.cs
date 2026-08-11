using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Customers;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateLeadDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    public string? CompanyName { get; set; }

    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(32)]
    public string? Phone { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public Guid? AssignedToUserId { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public bool DoNotContact { get; set; }

    public Guid? ReferrerAgentId { get; set; }

    [StringLength(128)]
    public string? Territory { get; set; }

    [StringLength(128)]
    public string? Cluster { get; set; }

    [StringLength(256)]
    public string? Location { get; set; }

    [StringLength(256)]
    public string? Estate { get; set; }

    [StringLength(64)]
    public string? ExternalAccountNumber { get; set; }

    [StringLength(64)]
    public string? ExternalTicketNumber { get; set; }
}
