namespace FinanceSentry.Infrastructure.Fx;

using FinanceSentry.Core.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolves a past date's conversion rate by routing to whichever feed publishes that
/// currency (UAH → NBU, ECB currencies → Frankfurter) and caching the result.
///
/// Published rates for a past date are immutable, so the cache only exists to avoid
/// re-fetching — it never has to be invalidated for correctness. When no feed can answer,
/// conversion degrades to today's flat rate from <see cref="CurrencyConverter"/>: a
/// slightly wrong historical figure beats an empty chart.
/// </summary>
public interface IHistoricalExchangeRateService
{
    /// <summary>
    /// USD-per-unit rates covering the inclusive range, gap-filled: feeds skip weekends and
    /// holidays, so each missing day carries the previous published rate forward (and days
    /// before the first published rate carry the first one backward).
    /// </summary>
    Task<IReadOnlyDictionary<DateOnly, decimal>> GetDailySeriesAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public sealed class CachingHistoricalExchangeRateService : IHistoricalExchangeRateService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(12);

    private readonly IReadOnlyList<IHistoricalExchangeRateProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingHistoricalExchangeRateService> _logger;

    public CachingHistoricalExchangeRateService(
        IEnumerable<IHistoricalExchangeRateProvider> providers,
        IMemoryCache cache,
        ILogger<CachingHistoricalExchangeRateService> logger)
    {
        _providers = providers.ToList();
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetDailySeriesAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currency) || to < from)
            return new Dictionary<DateOnly, decimal>();

        var normalized = currency.Trim().ToUpperInvariant();
        var cacheKey = $"fx-history:{normalized}:{from:yyyyMMdd}:{to:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<DateOnly, decimal>? cached) && cached is not null)
            return cached;

        var published = await FetchPublishedAsync(normalized, from, to, ct);
        var series = FillGaps(published, normalized, from, to);

        _cache.Set(cacheKey, series, CacheLifetime);
        return series;
    }

    private async Task<IReadOnlyDictionary<DateOnly, decimal>> FetchPublishedAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct)
    {
        foreach (var provider in _providers.Where(p => p.Supports(currency)))
        {
            var series = await provider.GetUsdPerUnitSeriesAsync(currency, from, to, ct);
            if (series.Count > 0)
                return series;
        }

        _logger.LogInformation(
            "No historical feed produced {Currency} rates for {From}..{To}; using the live rate for the whole window.",
            currency, from, to);
        return new Dictionary<DateOnly, decimal>();
    }

    private static IReadOnlyDictionary<DateOnly, decimal> FillGaps(
        IReadOnlyDictionary<DateOnly, decimal> published, string currency, DateOnly from, DateOnly to)
    {
        var fallback = CurrencyConverter.ToUsd(1m, currency);
        var seed = published.Count > 0
            ? published.OrderBy(kv => kv.Key).First().Value
            : fallback;

        var filled = new Dictionary<DateOnly, decimal>();
        var carried = seed;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (published.TryGetValue(date, out var rate))
                carried = rate;

            filled[date] = carried;
        }

        return filled;
    }
}
