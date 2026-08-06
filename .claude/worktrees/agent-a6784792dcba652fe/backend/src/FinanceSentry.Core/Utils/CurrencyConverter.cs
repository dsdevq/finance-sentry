namespace FinanceSentry.Core.Utils;

/// <summary>
/// Converts foreign amounts to USD using a process-wide rate table.
///
/// The table seeds with a small hardcoded fallback so conversion always works
/// offline (or before the first refresh). A background job replaces it with live
/// rates via <see cref="UpdateRates"/>. Rates are "USD per 1 unit" multipliers
/// (e.g. UAH ≈ 0.024), so <see cref="ToUsd"/> is a plain multiply.
///
/// Unknown currencies fall back to 1:1 — callers should treat totals mixing
/// unlisted currencies as approximate.
/// </summary>
public static class CurrencyConverter
{
    /// <summary>Offline seed. Also the source of truth when the refresh job has never run.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> FallbackRates =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1.00m,
            ["EUR"] = 1.08m,
            ["GBP"] = 1.27m,
            ["UAH"] = 0.024m,
        };

    // Immutable dictionary swapped by reference so reads never see a partial table.
    private static volatile IReadOnlyDictionary<string, decimal> _rates = FallbackRates;

    public static decimal ToUsd(decimal amount, string currency)
    {
        if (!string.IsNullOrWhiteSpace(currency) && _rates.TryGetValue(currency, out var rate))
            return amount * rate;

        return amount;
    }

    /// <summary>
    /// Replaces the live rate table (USD-per-unit multipliers). The hardcoded
    /// fallback is merged underneath so a currency briefly missing from the feed
    /// still resolves. Ignored when <paramref name="rates"/> is null/empty.
    /// </summary>
    public static void UpdateRates(IReadOnlyDictionary<string, decimal>? rates)
    {
        if (rates is null || rates.Count == 0)
            return;

        var merged = new Dictionary<string, decimal>(FallbackRates, StringComparer.OrdinalIgnoreCase);
        foreach (var (currency, rate) in rates)
        {
            if (rate > 0m)
                merged[currency] = rate;
        }

        merged["USD"] = 1.00m;
        _rates = merged;
    }
}
