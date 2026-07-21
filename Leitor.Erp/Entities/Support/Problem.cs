using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Support;

// ITIL4 Problem Management: the shared root cause behind one or more Tickets, tracked separately
// so a recurring fault gets diagnosed once instead of re-investigated from scratch every time a
// new Ticket surfaces it. Tickets link to a Problem via Ticket.ProblemId (optional, many-to-one) -
// a Problem has no FK back to any specific Ticket since it can explain several at once.
public class Problem : FullAuditedAggregateRoot<Guid>
{
    public string ProblemNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProblemStatus Status { get; set; } = ProblemStatus.Open;
    public string? RootCause { get; set; }
    public string? Workaround { get; set; }
    public DateTime IdentifiedDate { get; set; }

    // Auto-tracked the same way Ticket.ResolvedDate/WarrantyClaim.ResolvedDate already are - set
    // the moment Status transitions into Resolved/Closed, cleared if reopened.
    public DateTime? ResolvedDate { get; set; }

    protected Problem()
    {
    }

    public Problem(Guid id, string problemNumber, string title)
        : base(id)
    {
        ProblemNumber = problemNumber;
        Title = title;
    }
}
