using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Support;

// Append-only conversation thread, same rationale as Entities/Customers/CustomerNote.cs -
// CreatorId/CreationTime already give "who logged this and when". IsCustomerMessage mirrors
// eShopSupport's own Message entity: this ERP has no customer self-service login, so every
// message is entered by staff, but this flags which side of the conversation it represents.
public class TicketMessage : FullAuditedAggregateRoot<Guid>
{
    public Guid TicketId { get; set; }
    public bool IsCustomerMessage { get; set; }
    public string Text { get; set; } = string.Empty;

    protected TicketMessage()
    {
    }

    public TicketMessage(Guid id, Guid ticketId, bool isCustomerMessage, string text)
        : base(id)
    {
        TicketId = ticketId;
        IsCustomerMessage = isCustomerMessage;
        Text = text;
    }
}
