using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Governance;

// Deletion is permission-based but gated by approval: only a holder of
// ErpPermissions.DeletionApprovals.Decide (Admin/Ops Manager) can delete a scoped entity
// (Customer/Vendor/Order/Invoice/Ticket/FieldServiceJob/PurchaseOrder) immediately - see
// Services/Governance/DeletionGate.cs. Everyone else's delete action files one of these instead.
// EntityType/EntityId are a loose reference (string + Guid, no FK) since this spans several
// unrelated aggregate roots - not owned by any single existing module.
public class DeletionRequest : FullAuditedAggregateRoot<Guid>
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Reason { get; set; }
    public DeletionRequestStatus Status { get; set; } = DeletionRequestStatus.Pending;
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }

    protected DeletionRequest()
    {
    }

    public DeletionRequest(Guid id, string entityType, Guid entityId, Guid? requestedByUserId, DateTime requestedAt)
        : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        RequestedByUserId = requestedByUserId;
        RequestedAt = requestedAt;
    }
}
