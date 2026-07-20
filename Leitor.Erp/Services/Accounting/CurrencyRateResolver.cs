using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Shared by every document AppService that carries a CurrencyCode (Quote/Order/Invoice/Payment/
// PurchaseOrder/SupplierInvoice/VendorPayment) to snapshot an ExchangeRateToBase value at
// creation time - the rate is never recomputed later, exactly like TaxRateResolver snapshots
// TaxRatePercent onto a line. Returns 1 for the base currency itself without touching
// ExchangeRates at all.
public static class CurrencyRateResolver
{
    public static async Task<decimal> ResolveAsync(
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        string currencyCode,
        DateTime asOfDate)
    {
        var currency = (await currencyRepository.GetListAsync(x => x.Code == currencyCode)).FirstOrDefault();
        if (currency is { IsBaseCurrency: true })
        {
            return 1m;
        }

        var rate = (await exchangeRateRepository.GetListAsync(x => x.CurrencyCode == currencyCode && x.RateDate <= asOfDate.Date))
            .OrderByDescending(x => x.RateDate)
            .FirstOrDefault();

        if (rate == null)
        {
            throw new UserFriendlyException(
                $"No exchange rate is available for {currencyCode} on or before {asOfDate:yyyy-MM-dd}. Add a manual rate first.");
        }

        return rate.RateToBaseCurrency;
    }
}
