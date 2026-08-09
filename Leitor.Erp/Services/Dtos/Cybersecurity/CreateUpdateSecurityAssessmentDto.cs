using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Cybersecurity;

namespace Leitor.Erp.Services.Dtos.Cybersecurity;

public class CreateUpdateSecurityAssessmentDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    public SecurityAssessmentType Type { get; set; } = SecurityAssessmentType.VulnerabilityAssessment;

    public SecurityAssessmentStatus Status { get; set; } = SecurityAssessmentStatus.Scheduled;

    [Required]
    public DateTime ScheduledDate { get; set; }

    public Guid? ConductedByUserId { get; set; }

    public SecurityRiskRating? RiskRating { get; set; }

    [StringLength(4000)]
    public string? Findings { get; set; }

    [StringLength(4000)]
    public string? Recommendations { get; set; }

    public DateTime? FollowUpDate { get; set; }
}
