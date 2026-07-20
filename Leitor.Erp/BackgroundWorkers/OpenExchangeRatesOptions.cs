namespace Leitor.Erp.BackgroundWorkers;

// Bound from the "OpenExchangeRates" appsettings section - the real App Id is supplied via the
// OpenExchangeRates__AppId env var in Coolify, same convention as OpenIddict's certificate
// passphrase. Left blank in source control; ExchangeRateSyncWorker no-ops (logs and returns)
// rather than throwing when this is empty, since a missing third-party API key shouldn't be able
// to crash a background worker.
public class OpenExchangeRatesOptions
{
    public string AppId { get; set; } = string.Empty;
}
