namespace FinanceSentry.Infrastructure.Fx;

/// <summary>
/// Fetches live foreign-exchange rates as "USD per 1 unit" multipliers
/// (e.g. UAH ≈ 0.024), the shape <see cref="Core.Utils.CurrencyConverter"/> uses.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>Returns the latest USD-per-unit rates, or null if the fetch failed.</summary>
    Task<IReadOnlyDictionary<string, decimal>?> GetUsdRatesAsync(CancellationToken ct = default);
}
