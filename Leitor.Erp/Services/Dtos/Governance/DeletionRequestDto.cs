using System;
using Leitor.Erp.Entities.Governance;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Governance;

public class DeletionRequestDto : FullAuditedEntityDto<Guid>
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Reason { get; set; }
    public DeletionRequestStatus Status { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }

    // Resolved by DeletionRequestAppService from IdentityUser repository - not stored columns.
    public string? RequestedByUserName { get; set; }
    public string? DecidedByUserName { get; set; }
}
