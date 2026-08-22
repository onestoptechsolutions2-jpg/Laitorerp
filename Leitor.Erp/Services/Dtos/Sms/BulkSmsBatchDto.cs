using System;

namespace Leitor.Erp.Services.Dtos.Sms;

public class BulkSmsBatchDto
{
    public Guid BatchId { get; set; }
    public string ContentPreview { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public int TotalCount { get; set; }
    public int QueuedCount { get; set; }
    public int SendingCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
}
