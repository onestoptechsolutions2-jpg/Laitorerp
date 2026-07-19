using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Governance;

// An append-only log of business-meaningful moments across the whole sales/delivery chain -
// distinct from Volo.Abp.AuditLogging's generic entity-property-change tracking (which already
// captures every Create/Update/Delete automatically). This exists for the things that tracking
// can't express: which stage a document reached, who sent a Proposal and over which channel, why
// an approved document was unlocked for revision. EntityType/EntityId are a loose reference (no
// FK) since this spans many unrelated aggregate roots - same rationale as DeletionRequest.
public class WorkflowStageEvent : FullAuditedAggregateRoot<Guid>
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public WorkflowStage Stage { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? UserId { get; set; }

    // "Email"/"WhatsApp" - only meaningful for ProposalSent; null otherwise.
    public string? Channel { get; set; }
    public string? Notes { get; set; }

    protected WorkflowStageEvent()
    {
    }

    public WorkflowStageEvent(Guid id, string entityType, Guid entityId, WorkflowStage stage, DateTime occurredAt)
        : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        Stage = stage;
        OccurredAt = occurredAt;
    }
}
