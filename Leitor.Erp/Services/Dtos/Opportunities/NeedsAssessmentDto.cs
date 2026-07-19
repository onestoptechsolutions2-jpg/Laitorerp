using System;
using Leitor.Erp.Entities.Opportunities;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class NeedsAssessmentDto : FullAuditedEntityDto<Guid>
{
    public Guid OpportunityId { get; set; }
    public NeedsAssessmentType Type { get; set; }
    public DateTime ConductedDate { get; set; }
    public Guid? ConductedByUserId { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string? Risks { get; set; }
    public string? Recommendations { get; set; }
    public string? CustomerRequirements { get; set; }

    // Resolved by NeedsAssessmentAppService - not a stored column.
    public string? ConductedByUserName { get; set; }
}
