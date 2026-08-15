namespace Leitor.Erp.Entities.Governance;

public enum EscalationItemStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,

    // Unlike DeletionRequestStatus (3 states - a delete basically can't fail), an escalated
    // action's handler re-validates in full at approval time and can legitimately fail if
    // something drifted since filing. See EscalationItemAppService.ApproveAsync.
    Failed = 3
}
