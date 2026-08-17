using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// Append-only activity log entry (a "note" in Twenty CRM's terms - see NoteTargets relation).
// No Update is exposed anywhere: CreatorId/CreationTime (from FullAuditedAggregateRoot) already
// give the "who logged this and when" a timeline needs. Direction/TouchedAt were added to make
// this double as the customer-facing engagement log (mirrors LeadTouch's exact reasoning: a call
// or WhatsApp reply is often logged after the fact, so TouchedAt - "when the contact happened" -
// is kept separate from CreationTime - "when it was entered into the system").
public class CustomerNote : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public CustomerNoteType Type { get; set; } = CustomerNoteType.General;
    public LeadDirection Direction { get; set; } = LeadDirection.Outbound;
    public string Text { get; set; } = string.Empty;
    public DateTime TouchedAt { get; set; }

    protected CustomerNote()
    {
    }

    public CustomerNote(Guid id, Guid customerId, CustomerNoteType type, LeadDirection direction, string text, DateTime touchedAt)
        : base(id)
    {
        CustomerId = customerId;
        Type = type;
        Direction = direction;
        Text = text;
        TouchedAt = touchedAt;
    }
}
