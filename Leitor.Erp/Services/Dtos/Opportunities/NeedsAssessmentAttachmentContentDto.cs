using System;

namespace Leitor.Erp.Services.Dtos.Opportunities;

// Used only by the Download page - kept separate from NeedsAssessmentAttachmentDto so list views
// never pull file bytes into memory.
public class NeedsAssessmentAttachmentContentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
