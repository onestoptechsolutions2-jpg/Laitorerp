using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Leitor.Erp.BackgroundWorkers;

// Runs once daily: fetches USD-based rates from Open Exchange Rates and derives each tracked
// non-base Currency's rate to the base currency (KES) from them - the free tier only supports
// USD as the request base, so every other currency's rate to base is computed as
// rates[BaseCode] / rates[Code] (USD's own rate to base is just rates[BaseCode]). Upserts today's
// ExchangeRate row per currency directly through the repository (not through
// ExchangeRateAppService, since there's no HTTP user in a background worker context). A missing/
// invalid AppId or a failed API call is logged and skipped, never thrown - a third-party outage
// shouldn't be able to crash a background worker.
public class ExchangeRateSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ExchangeRateSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var logger = workerContext.ServiceProvider.GetRequiredService<ILogger<ExchangeRateSyncWorker>>();
        var options = workerContext.ServiceProvider.GetRequiredService<IOptions<OpenExchangeRatesOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.AppId))
        {
            logger.LogWarning("OpenExchangeRates:AppId is not configured - skipping today's exchange rate sync.");
            return;
        }

        var currencyRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Currency, Guid>>();
        var exchangeRateRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<ExchangeRate, Guid>>();
        var httpClientFactory = workerContext.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var guidGenerator = workerContext.ServiceProvider.GetRequiredService<IGuidGenerator>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();

        var baseCurrency = (await currencyRepository.GetListAsync(x => x.IsBaseCurrency)).FirstOrDefault();
        var trackedCurrencies = (await currencyRepository.GetListAsync(x => x.IsActive && !x.IsBaseCurrency)).ToList();
        if (baseCurrency == null || trackedCurrencies.Count == 0)
        {
            return;
        }

        Dictionary<string, decimal> usdRates;
        try
        {
            var httpClient = httpClientFactory.CreateClient("OpenExchangeRates");
            var response = await httpClient.GetAsync($"https://openexchangerates.org/api/latest.json?app_id={options.AppId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            usdRates = document.RootElement.GetProperty("rates")
                .EnumerateObject()
                .ToDictionary(x => x.Name, x => x.Value.GetDecimal());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch exchange rates from Open Exchange Rates - skipping today's sync.");
            return;
        }

        if (!usdRates.TryGetValue(baseCurrency.Code, out var usdToBase))
        {
            logger.LogWarning(
                "Open Exchange Rates response did not include the base currency {BaseCurrency} - skipping today's sync.",
                baseCurrency.Code);
            return;
        }

        var today = clock.Now.Date;
        var existingTodayRates = (await exchangeRateRepository.GetListAsync(x => x.RateDate == today))
            .ToDictionary(x => x.CurrencyCode);

        var newRates = new List<ExchangeRate>();
        var updatedRates = new List<ExchangeRate>();

        foreach (var currency in trackedCurrencies)
        {
            decimal rateToBase;
            if (currency.Code == "USD")
            {
                rateToBase = usdToBase;
            }
            else if (usdRates.TryGetValue(currency.Code, out var usdToCurrency) && usdToCurrency > 0)
            {
                rateToBase = usdToBase / usdToCurrency;
            }
            else
            {
                logger.LogWarning("Open Exchange Rates response did not include {CurrencyCode} - skipping it today.", currency.Code);
                continue;
            }

            if (existingTodayRates.TryGetValue(currency.Code, out var existing))
            {
                existing.RateToBaseCurrency = rateToBase;
                existing.Source = "OpenExchangeRates";
                updatedRates.Add(existing);
            }
            else
            {
                newRates.Add(new ExchangeRate(guidGenerator.Create(), currency.Code, today, rateToBase, "OpenExchangeRates"));
            }
        }

        if (newRates.Count > 0)
        {
            await exchangeRateRepository.InsertManyAsync(newRates);
        }

        if (updatedRates.Count > 0)
        {
            await exchangeRateRepository.UpdateManyAsync(updatedRates);
        }
    }
}
