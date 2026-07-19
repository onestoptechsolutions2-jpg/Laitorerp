using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Opportunities;

// Same shape as Entities/Customers/CustomerAttachment.cs - file bytes stored directly in Postgres,
// same rationale (piggybacks on infrastructure already proven working, not a volume/object store).
public class NeedsAssessmentAttachment : FullAuditedAggregateRoot<Guid>
{
    public Guid NeedsAssessmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();

    protected NeedsAssessmentAttachment()
    {
    }

    public NeedsAssessmentAttachment(Guid id, Guid needsAssessmentId, string fileName, string contentType, byte[] content)
        : base(id)
    {
        NeedsAssessmentId = needsAssessmentId;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
    }
}
