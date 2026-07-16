using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// Append-only activity log entry (a "note" in Twenty CRM's terms - see NoteTargets relation).
// No Update is exposed anywhere: CreatorId/CreationTime (from FullAuditedAggregateRoot) already
// give the "who logged this and when" a timeline needs, so this entity only needs Type + Text.
public class CustomerNote : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public CustomerNoteType Type { get; set; } = CustomerNoteType.General;
    public string Text { get; set; } = string.Empty;

    protected CustomerNote()
    {
    }

    public CustomerNote(Guid id, Guid customerId, CustomerNoteType type, string text)
        : base(id)
    {
        CustomerId = customerId;
        Type = type;
        Text = text;
    }
}
