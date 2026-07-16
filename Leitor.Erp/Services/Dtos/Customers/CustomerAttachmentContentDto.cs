using System;

namespace Leitor.Erp.Services.Dtos.Customers;

// Used only by the Download page - kept separate from CustomerAttachmentDto so list views never
// pull file bytes into memory.
public class CustomerAttachmentContentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
