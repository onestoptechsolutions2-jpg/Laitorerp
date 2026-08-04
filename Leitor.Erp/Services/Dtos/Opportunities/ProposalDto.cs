using System;
using Leitor.Erp.Entities.Opportunities;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class ProposalDto : FullAuditedEntityDto<Guid>
{
    public Guid OpportunityId { get; set; }
    public string ProposalNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; }
    public int Version { get; set; }
    public bool IsLocked { get; set; }
    public Guid? UnlockedByUserId { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }

    public string? Summary { get; set; }
    public string? ProposedSolution { get; set; }
    public string? Scope { get; set; }
    public string? Timeline { get; set; }
    public string? Assumptions { get; set; }
    public string? Exclusions { get; set; }
    public string? WarrantyAndSupport { get; set; }
    public string? Terms { get; set; }
}
