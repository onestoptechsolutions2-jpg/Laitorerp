namespace Leitor.Erp.Entities.Support;

// Resolved and Closed are both terminal "no longer active work" outcomes (ResolvedDate is set on
// either) - kept as two distinct statuses since "fix applied, awaiting confirmation" vs
// "fully done/archived" is a meaningful, common support-ticket distinction.
public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    WaitingOnCustomer = 2,
    Resolved = 3,
    Closed = 4
}
