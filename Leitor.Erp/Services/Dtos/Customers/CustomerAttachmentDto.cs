using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

// Deliberately excludes the file bytes - list views shouldn't load large blobs into memory.
// Download.cshtml.cs fetches the entity/bytes directly via the repository when actually needed.
public class CustomerAttachmentDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Resolved by CustomerAttachmentAppService from IIdentityUserRepository - not a stored column.
    public string? UploadedByUserName { get; set; }
}
