using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Opportunities;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class CreateUpdateProposalDto
{
    [Required]
    public Guid OpportunityId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;

    [StringLength(4000)]
    public string? Summary { get; set; }

    [StringLength(4000)]
    public string? ProposedSolution { get; set; }

    [StringLength(4000)]
    public string? Scope { get; set; }

    [StringLength(2000)]
    public string? Timeline { get; set; }

    [StringLength(2000)]
    public string? Assumptions { get; set; }

    [StringLength(2000)]
    public string? Exclusions { get; set; }

    [StringLength(2000)]
    public string? WarrantyAndSupport { get; set; }

    [StringLength(2000)]
    public string? Terms { get; set; }
}
