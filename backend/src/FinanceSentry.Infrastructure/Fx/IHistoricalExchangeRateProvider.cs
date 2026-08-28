namespace FinanceSentry.Infrastructure.Fx;

/// <summary>
/// Fetches past foreign-exchange rates as "USD per 1 unit" multipliers — the same shape
/// <see cref="Core.Utils.CurrencyConverter"/> uses for today's rates, so a historical
/// amount converts identically to a live one.
///
/// Published rates for a past date never change, so implementations are free to cache
/// aggressively.
/// </summary>
public interface IHistoricalExchangeRateProvider
{
    /// <summary>True when this provider publishes rates for <paramref name="currency"/>.</summary>
    bool Supports(string currency);

    /// <summary>
    /// USD-per-unit rates for each published day in the inclusive range. Days the source
    /// skips (weekends, holidays) are simply absent — callers should carry the last known
    /// rate forward rather than assume a gap means zero. Returns an empty series when the
    /// feed is unreachable; callers fall back to the flat live rate.
    /// </summary>
    Task<IReadOnlyDictionary<DateOnly, decimal>> GetUsdPerUnitSeriesAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct = default);
}
