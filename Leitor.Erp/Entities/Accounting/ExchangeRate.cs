using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Accounting;

// One row per (CurrencyCode, RateDate) - RateToBaseCurrency is "1 unit of CurrencyCode equals
// this many units of the base currency". Rows come either from the daily ExchangeRateSyncWorker
// (Source = "OpenExchangeRates") or a manual correction/backfill entry (Source = "Manual").
// CurrencyRateResolver always reads the latest row at-or-before a given date and snapshots it
// onto the document being created - this table itself is never queried again for that document.
public class ExchangeRate : FullAuditedAggregateRoot<Guid>
{
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime RateDate { get; set; }
    public decimal RateToBaseCurrency { get; set; }
    public string Source { get; set; } = string.Empty;

    protected ExchangeRate()
    {
    }

    public ExchangeRate(Guid id, string currencyCode, DateTime rateDate, decimal rateToBaseCurrency, string source)
        : base(id)
    {
        CurrencyCode = currencyCode;
        RateDate = rateDate;
        RateToBaseCurrency = rateToBaseCurrency;
        Source = source;
    }
}
