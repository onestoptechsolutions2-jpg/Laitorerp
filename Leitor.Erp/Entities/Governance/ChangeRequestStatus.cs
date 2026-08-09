namespace Leitor.Erp.Entities.Governance;

public enum ChangeRequestStatus
{
    Draft = 0,

    // Normal-tier only - Standard and Emergency changes never pass through this state (see
    // ChangeRequestAppService.MapToEntityAsync).
    PendingApproval = 1,

    Approved = 2,
    Rejected = 3,
    Completed = 4,

    // The change was implemented but had to be reverted - a distinct terminal state from
    // Completed, not just a note field, since "how often do our changes get rolled back" is
    // exactly the kind of signal Continual Improvement should be able to trend on.
    RolledBack = 5
}
