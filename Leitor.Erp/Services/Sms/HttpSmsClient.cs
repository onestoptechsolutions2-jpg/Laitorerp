using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Leitor.Erp.Settings;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Leitor.Erp.Services.Sms;

// Thin wrapper around the hosted httpSMS API (https://api.httpsms.com/v1) - confirmed contract:
// POST /messages/send, header "x-api-key: <key>", JSON body { from, to, content }, both numbers
// E.164. Never throws - same "log and return a failure result, let the caller decide what to do"
// convention as ExchangeRateSyncWorker's third-party HTTP call, since a gateway-phone or API
// outage shouldn't crash a background dispatch tick or a page handler.
public class HttpSmsClient : IHttpSmsClient, ITransientDependency
{
    private readonly ISettingProvider _settingProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpSmsClient> _logger;

    public HttpSmsClient(
        ISettingProvider settingProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpSmsClient> logger)
    {
        _settingProvider = settingProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var apiKey = await _settingProvider.GetOrNullAsync(ErpSettings.BulkSmsApiKey);
        var fromNumber = await _settingProvider.GetOrNullAsync(ErpSettings.BulkSmsFromNumber);
        return !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(fromNumber);
    }

    public async Task<HttpSmsSendResult> SendAsync(string toE164, string content, CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingProvider.GetOrNullAsync(ErpSettings.BulkSmsApiKey);
        var fromNumber = await _settingProvider.GetOrNullAsync(ErpSettings.BulkSmsFromNumber);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromNumber))
        {
            return new HttpSmsSendResult
            {
                Success = false,
                ErrorMessage = "Bulk SMS is not configured - set the API key and sender number under Administration > Operations > Bulk SMS."
            };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("HttpSms");
            using var request = new HttpRequestMessage(HttpMethod.Post, "messages/send")
            {
                Content = JsonContent.Create(new { from = fromNumber, to = toE164, content })
            };
            request.Headers.Add("x-api-key", apiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new HttpSmsSendResult
                {
                    Success = false,
                    ErrorMessage = $"httpSMS returned {(int)response.StatusCode}: {Truncate(body, 500)}"
                };
            }

            string? providerMessageId = null;
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var idProperty))
                {
                    providerMessageId = idProperty.GetString();
                }
            }
            catch (JsonException)
            {
                // Body wasn't the shape we expected - still a 2xx, so treat the send as
                // successful, just without a provider id to track.
            }

            return new HttpSmsSendResult { Success = true, ProviderMessageId = providerMessageId };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SMS via httpSMS to {To}", toE164);
            return new HttpSmsSendResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
