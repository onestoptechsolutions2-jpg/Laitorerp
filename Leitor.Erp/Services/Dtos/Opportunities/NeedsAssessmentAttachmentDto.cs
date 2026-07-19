using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

// Deliberately excludes the file bytes - list views shouldn't load large blobs into memory.
// Download.cshtml.cs fetches the entity/bytes directly via the repository when actually needed.
public class NeedsAssessmentAttachmentDto : FullAuditedEntityDto<Guid>
{
    public Guid NeedsAssessmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Resolved by NeedsAssessmentAttachmentAppService from IIdentityUserRepository - not a stored column.
    public string? UploadedByUserName { get; set; }
}
