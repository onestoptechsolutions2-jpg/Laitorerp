using System;
using Leitor.Erp.Entities.Cybersecurity;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Cybersecurity;

public class SecurityAssessmentDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string AssessmentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SecurityAssessmentType Type { get; set; }
    public SecurityAssessmentStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public Guid? ConductedByUserId { get; set; }
    public SecurityRiskRating? RiskRating { get; set; }
    public string? Findings { get; set; }
    public string? Recommendations { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Resolved by SecurityAssessmentAppService from Customer/IdentityUser repositories - not
    // stored columns.
    public string? CustomerName { get; set; }
    public string? ConductedByUserName { get; set; }
}
