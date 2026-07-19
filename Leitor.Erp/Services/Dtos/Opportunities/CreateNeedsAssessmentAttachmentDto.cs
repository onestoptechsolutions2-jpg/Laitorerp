using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class CreateNeedsAssessmentAttachmentDto
{
    [Required]
    public Guid NeedsAssessmentId { get; set; }

    [Required]
    [StringLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
