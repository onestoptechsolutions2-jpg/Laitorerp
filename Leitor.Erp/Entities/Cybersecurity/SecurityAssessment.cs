using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Cybersecurity;

// The "eventually: Cybersecurity" upsell tier from Laitor's stated business model - vulnerability
// assessments, cyber-risk assessments, security policy reviews, cyber awareness training, and
// backup/DR reviews, all tracked the same way against a Customer rather than getting five separate
// entities. Closest existing shape this was cloned from: NeedsAssessment (Opportunities module).
public class SecurityAssessment : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public string AssessmentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SecurityAssessmentType Type { get; set; } = SecurityAssessmentType.VulnerabilityAssessment;
    public SecurityAssessmentStatus Status { get; set; } = SecurityAssessmentStatus.Scheduled;
    public DateTime ScheduledDate { get; set; }
    public Guid? ConductedByUserId { get; set; }
    public SecurityRiskRating? RiskRating { get; set; }
    public string? Findings { get; set; }
    public string? Recommendations { get; set; }
    public DateTime? FollowUpDate { get; set; }

    // Auto-tracked the same way Problem.ResolvedDate/Ticket.ResolvedDate already are - set the
    // moment Status transitions into Completed, cleared if moved back out.
    public DateTime? CompletedDate { get; set; }

    protected SecurityAssessment()
    {
    }

    public SecurityAssessment(Guid id, Guid customerId, string assessmentNumber, string title)
        : base(id)
    {
        CustomerId = customerId;
        AssessmentNumber = assessmentNumber;
        Title = title;
    }
}
