using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Sms;

// One row per recipient of a bulk SMS send, dispatched via the hosted httpSMS API (see
// Services/Sms/HttpSmsClient.cs). BatchId groups every row queued together in one "send" action -
// a flat log with a grouping key, not a separate Campaign+Recipient model, since nothing here
// needs campaign-level fields (name, schedule, etc.) beyond what a GROUP BY BatchId query already
// gives the history view (see BulkSmsAppService.GetBatchesAsync).
public class BulkSmsMessage : FullAuditedAggregateRoot<Guid>
{
    public Guid BatchId { get; set; }
    public string ToPhoneNumber { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public BulkSmsRecipientType RecipientType { get; set; }
    public Guid? RecipientEntityId { get; set; }
    public string? RecipientName { get; set; }
    public BulkSmsMessageStatus Status { get; set; } = BulkSmsMessageStatus.Queued;
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }

    protected BulkSmsMessage()
    {
    }

    public BulkSmsMessage(Guid id, Guid batchId, string toPhoneNumber, string content, BulkSmsRecipientType recipientType)
        : base(id)
    {
        BatchId = batchId;
        ToPhoneNumber = toPhoneNumber;
        Content = content;
        RecipientType = recipientType;
    }
}
