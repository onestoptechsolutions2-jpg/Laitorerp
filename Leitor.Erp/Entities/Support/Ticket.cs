using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Support;

public class Ticket : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? ContractId { get; set; }

    // Root-cause grouping (ITIL4 Problem Management) - several Tickets can share one Problem
    // rather than each being independently re-diagnosed. Optional and set manually once a pattern
    // is noticed; never inferred automatically.
    public Guid? ProblemId { get; set; }

    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketType Type { get; set; } = TicketType.General;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public int? CustomerSatisfactionRating { get; set; }

    // Set once at creation from Priority via a fixed hours table (see TicketAppService) - never
    // recomputed on update, so changing Priority later doesn't retroactively move the target.
    // Breach itself is computed, not stored (see TicketDto.IsSlaBreached), same convention as
    // InvoicePaymentStatus.
    public DateTime? SlaDueDate { get; set; }

    // Incremented in TicketAppService.CopyToEntity every time Status moves from a terminal state
    // (Resolved/Closed) back to a non-terminal one - the Continual Improvement signal the 2026-07-21
    // ITSM audit flagged as missing (SupportAnalyticsAppService.GetReopenRateTrendAsync reports on it).
    public int ReopenCount { get; set; }

    protected Ticket()
    {
    }

    public Ticket(Guid id, Guid customerId, string ticketNumber, string subject)
        : base(id)
    {
        CustomerId = customerId;
        TicketNumber = ticketNumber;
        Subject = subject;
    }
}
