using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Opportunities;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class CreateUpdateOpportunityDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    public decimal? EstimatedValue { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }

    public Guid? AssignedToUserId { get; set; }

    [StringLength(2000)]
    public string? LostReason { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
