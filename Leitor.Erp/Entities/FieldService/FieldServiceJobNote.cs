using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.FieldService;

// Append-only visit log, same shape and rationale as Entities/Customers/CustomerNote.cs -
// CreatorId/CreationTime already give "who logged this and when", no Update needed anywhere.
public class FieldServiceJobNote : FullAuditedAggregateRoot<Guid>
{
    public Guid JobId { get; set; }
    public FieldServiceJobNoteType Type { get; set; } = FieldServiceJobNoteType.General;
    public string Text { get; set; } = string.Empty;

    protected FieldServiceJobNote()
    {
    }

    public FieldServiceJobNote(Guid id, Guid jobId, FieldServiceJobNoteType type, string text)
        : base(id)
    {
        JobId = jobId;
        Type = type;
        Text = text;
    }
}
