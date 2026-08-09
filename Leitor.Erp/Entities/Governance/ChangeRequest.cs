using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Governance;

// ITIL Change Enablement: a deliberate change to a live ConfigurationItem (a patch, a config
// change, a migration), tracked separately from Ticket - a Ticket models something reported as
// broken, this models something the business chose to change. Lives in Entities/Governance
// alongside DeletionRequest/WorkflowStageEvent since it's fundamentally a governed-action record,
// not a property of the asset itself.
public class ChangeRequest : FullAuditedAggregateRoot<Guid>
{
    public Guid ConfigurationItemId { get; set; }
    public string ChangeNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChangeTier Tier { get; set; } = ChangeTier.Normal;
    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Draft;

    public Guid? TicketId { get; set; }
    public DateTime? PlannedDate { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }

    // Auto-tracked the same way Problem.ResolvedDate/ServiceRequest.FulfilledDate already are.
    public DateTime? CompletedDate { get; set; }

    public bool RolledBack { get; set; }
    public string? RollbackNotes { get; set; }

    // Emergency changes proceed before approval, but must be reviewed after the fact - this is
    // that review's timestamp, mandatory per ITIL guidance for the Emergency tier specifically
    // (Standard and Normal changes don't need one - they were already reviewed before the work).
    public DateTime? PostImplementationReviewedDate { get; set; }

    protected ChangeRequest()
    {
    }

    public ChangeRequest(Guid id, Guid configurationItemId, string changeNumber, string title)
        : base(id)
    {
        ConfigurationItemId = configurationItemId;
        ChangeNumber = changeNumber;
        Title = title;
    }
}
