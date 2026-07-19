using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Opportunities;

public class NeedsAssessment : FullAuditedAggregateRoot<Guid>
{
    public Guid OpportunityId { get; set; }
    public NeedsAssessmentType Type { get; set; } = NeedsAssessmentType.SiteSurvey;
    public DateTime ConductedDate { get; set; }
    public Guid? ConductedByUserId { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string? Risks { get; set; }
    public string? Recommendations { get; set; }
    public string? CustomerRequirements { get; set; }

    protected NeedsAssessment()
    {
    }

    public NeedsAssessment(Guid id, Guid opportunityId)
        : base(id)
    {
        OpportunityId = opportunityId;
    }
}
