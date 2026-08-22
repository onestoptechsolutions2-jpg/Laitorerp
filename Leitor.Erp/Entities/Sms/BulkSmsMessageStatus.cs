namespace Leitor.Erp.Entities.Sms;

// No "Delivered" - that needs httpSMS's inbound delivery-receipt webhook, which this integration
// doesn't wire up (see HttpSmsClient). "Sent" here means the httpSMS API accepted the message,
// not that the handset confirmed delivery.
public enum BulkSmsMessageStatus
{
    Queued = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3
}
