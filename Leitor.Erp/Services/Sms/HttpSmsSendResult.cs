namespace Leitor.Erp.Services.Sms;

public class HttpSmsSendResult
{
    public bool Success { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? ErrorMessage { get; init; }
}
