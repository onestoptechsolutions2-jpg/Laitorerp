using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Support;

public class Ticket : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? ContractId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketType Type { get; set; } = TicketType.General;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public int? CustomerSatisfactionRating { get; set; }

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
