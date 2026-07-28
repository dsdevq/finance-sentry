namespace FinanceSentry.Infrastructure.Fx;

using FinanceSentry.Core.Utils;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hangfire job that pulls live FX rates and swaps them into
/// <see cref="CurrencyConverter"/>. On failure the existing table is kept, so a
/// feed outage degrades to stale rates rather than breaking conversion.
/// </summary>
public sealed class ExchangeRateRefreshJob
{
    private readonly IExchangeRateProvider _provider;
    private readonly ILogger<ExchangeRateRefreshJob> _logger;

    public ExchangeRateRefreshJob(
        IExchangeRateProvider provider, ILogger<ExchangeRateRefreshJob> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var rates = await _provider.GetUsdRatesAsync(ct);
        if (rates is null)
        {
            _logger.LogWarning("Exchange rate refresh produced no rates; leaving current table in place.");
            return;
        }

        CurrencyConverter.UpdateRates(rates);
    }
}
