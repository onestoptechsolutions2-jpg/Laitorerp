using System;
using Leitor.Erp.Entities.Sms;

namespace Leitor.Erp.Services.Dtos.Sms;

public class BulkSmsMessageDto
{
    public Guid Id { get; set; }
    public string ToPhoneNumber { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public BulkSmsRecipientType RecipientType { get; set; }
    public BulkSmsMessageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
}
