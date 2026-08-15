using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Governance;

// Generalizes DeletionRequest for actions that aren't a delete: unlike a delete, an escalated
// action needs parameters (PayloadJson) and can be gated by different approver permissions
// depending on ActionType (RequiredPermission), not one global permission. See
// Services/Governance/EscalationGate.cs / EscalationItemAppService.cs / IEscalationActionHandler.
public class EscalationItem : FullAuditedAggregateRoot<Guid>
{
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    // The permission a decider must hold to approve/reject this specific item - e.g.
    // ErpPermissions.Sales.OverrideMarginGate for a "Quote.MarginOverride" item. Checked
    // dynamically in EscalationItemAppService.CanDecideAsync rather than via a static
    // [Authorize(...)] attribute, since it varies per row.
    public string RequiredPermission { get; set; } = string.Empty;

    // System.Text.Json-serialized, handler-specific - e.g. MarginOverridePayload for the margin
    // gate's own action types. Null if the action needs no parameters beyond identity.
    public string? PayloadJson { get; set; }

    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Reason { get; set; }
    public EscalationItemStatus Status { get; set; } = EscalationItemStatus.Pending;
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }

    // Populated only when Status == Failed - the handler's re-validation threw at approval time
    // (e.g. the underlying document's state drifted since filing). See
    // EscalationItemAppService.ApproveAsync.
    public string? ExecutionError { get; set; }

    protected EscalationItem()
    {
    }

    public EscalationItem(
        Guid id, string actionType, string entityType, Guid entityId, string requiredPermission,
        string? payloadJson, Guid? requestedByUserId, DateTime requestedAt, string? reason)
        : base(id)
    {
        ActionType = actionType;
        EntityType = entityType;
        EntityId = entityId;
        RequiredPermission = requiredPermission;
        PayloadJson = payloadJson;
        RequestedByUserId = requestedByUserId;
        RequestedAt = requestedAt;
        Reason = reason;
    }
}
