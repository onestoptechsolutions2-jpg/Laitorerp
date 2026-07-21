namespace Leitor.Erp.Entities.Support;

// ITIL4: KnownError is a distinct status from Investigating - it means root cause is identified
// and a workaround exists, but the underlying fix hasn't shipped yet. Resolved/Closed mirror
// TicketStatus's own "fix applied, awaiting confirmation" vs "fully done" distinction.
public enum ProblemStatus
{
    Open = 0,
    Investigating = 1,
    KnownError = 2,
    Resolved = 3,
    Closed = 4
}
