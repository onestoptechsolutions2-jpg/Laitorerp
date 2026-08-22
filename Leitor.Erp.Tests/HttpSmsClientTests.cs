using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Leitor.Erp.Services.Sms;
using Leitor.Erp.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Settings;
using Xunit;

namespace Leitor.Erp.Tests;

// Pure unit test - no ErpTestBase/real network call, just a fake HttpMessageHandler standing in
// for the hosted httpSMS API (see HttpSmsClient for the confirmed request/response contract).
public class HttpSmsClientTests
{
    [Fact]
    public async Task SendAsync_Parses_ProviderMessageId_On_Success()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":{\"id\":\"msg-123\",\"status\":\"sent\"},\"status\":\"success\"}")
        });

        var client = CreateClient(handler, apiKey: "test-key", fromNumber: "+18005550199");
        var result = await client.SendAsync("+18005550100", "Hello");

        Assert.True(result.Success);
        Assert.Equal("msg-123", result.ProviderMessageId);
        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("x-api-key").Single());
    }

    [Fact]
    public async Task SendAsync_Returns_Failure_On_Non_Success_Status()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"invalid api key\"}")
        });

        var client = CreateClient(handler, apiKey: "bad-key", fromNumber: "+18005550199");
        var result = await client.SendAsync("+18005550100", "Hello");

        Assert.False(result.Success);
        Assert.Contains("401", result.ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_Returns_Failure_When_Not_Configured()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler, apiKey: null, fromNumber: null);

        var result = await client.SendAsync("+18005550100", "Hello");

        Assert.False(result.Success);
        Assert.False(handler.WasCalled);
    }

    private static HttpSmsClient CreateClient(FakeHttpMessageHandler handler, string? apiKey, string? fromNumber)
    {
        var settings = new Dictionary<string, string?>
        {
            [ErpSettings.BulkSmsApiKey] = apiKey,
            [ErpSettings.BulkSmsFromNumber] = fromNumber
        };
        var settingProvider = new FakeSettingProvider(settings);
        var httpClientFactory = new FakeHttpClientFactory(handler);
        return new HttpSmsClient(settingProvider, httpClientFactory, NullLogger<HttpSmsClient>.Instance);
    }

    private class FakeSettingProvider : ISettingProvider
    {
        private readonly Dictionary<string, string?> _values;

        public FakeSettingProvider(Dictionary<string, string?> values)
        {
            _values = values;
        }

        public Task<string?> GetOrNullAsync(string name)
        {
            return Task.FromResult(_values.TryGetValue(name, out var value) ? value : null);
        }

        public Task<List<SettingValue>> GetAllAsync(string[] names)
        {
            return Task.FromResult(new List<SettingValue>());
        }

        public Task<List<SettingValue>> GetAllAsync()
        {
            return Task.FromResult(new List<SettingValue>());
        }
    }

    private class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.httpsms.com/v1/")
            };
        }
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public HttpRequestMessage? LastRequest { get; private set; }
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            LastRequest = request;
            return Task.FromResult(_respond(request));
        }
    }
}
