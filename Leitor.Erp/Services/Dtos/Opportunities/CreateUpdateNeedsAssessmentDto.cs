using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Opportunities;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class CreateUpdateNeedsAssessmentDto
{
    [Required]
    public Guid OpportunityId { get; set; }

    public NeedsAssessmentType Type { get; set; } = NeedsAssessmentType.SiteSurvey;

    [Required]
    public DateTime ConductedDate { get; set; }

    public Guid? ConductedByUserId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Findings { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Risks { get; set; }

    [StringLength(4000)]
    public string? Recommendations { get; set; }

    [StringLength(4000)]
    public string? CustomerRequirements { get; set; }
}
