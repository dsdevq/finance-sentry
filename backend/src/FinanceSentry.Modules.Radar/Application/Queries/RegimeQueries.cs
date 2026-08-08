namespace FinanceSentry.Modules.Radar.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Domain.Regime;
using FinanceSentry.Modules.Radar.Domain.Repositories;

// ── get_market_regime ───────────────────────────────────────────────────────

/// <summary>Reads the latest persisted regime — both axes, raw readings, per-axis last change.</summary>
public sealed record GetMarketRegimeQuery : IQuery<RegimeStateDto>;

public sealed class GetMarketRegimeQueryHandler(IRegimeReadingRepository readings)
    : IQueryHandler<GetMarketRegimeQuery, RegimeStateDto>
{
    // Bounded history scan for the last-change lookup — a band changes far less often than daily.
    private const int RecentScanLimit = 400;

    public async Task<RegimeStateDto> Handle(GetMarketRegimeQuery query, CancellationToken cancellationToken)
    {
        var latest = await readings.LatestAsync(cancellationToken);
        if (latest is null)
        {
            return RegimeStateDto.Empty;
        }

        var recent = await readings.RecentAsync(RecentScanLimit, cancellationToken);

        var volatility = new VolatilityAxisDto(
            latest.VolatilityAvailable,
            latest.VolatilityRegime?.ToString(),
            latest.VixLevel,
            latest.VixSma,
            latest.VixTrend?.ToString(),
            latest.VolatilityAvailable
                ? LastChange(recent, r => r.VolatilityAvailable, r => r.VolatilityRegime?.ToString())
                : null);

        var rates = new RatesAxisDto(
            latest.RatesAvailable,
            latest.RatesRegime?.ToString(),
            latest.Dgs10,
            latest.Dgs2,
            latest.Spread,
            latest.RecessionWarning,
            latest.GrowthValueTilt,
            latest.RatesAvailable
                ? LastChange(recent, r => r.RatesAvailable, r => r.RatesRegime?.ToString())
                : null);

        return new RegimeStateDto(latest.ComputedAt, volatility, rates);
    }

    /// <summary>
    /// The <c>ComputedAt</c> of the most-recent reading whose band differs from the next-older
    /// reading (on the same axis, both available). Null when the axis has never changed within the
    /// scanned window. <paramref name="recent"/> is newest-first.
    /// </summary>
    private static DateTimeOffset? LastChange(
        IReadOnlyList<RegimeReading> recent,
        Func<RegimeReading, bool> available,
        Func<RegimeReading, string?> band)
    {
        var series = recent.Where(available).ToList();
        for (var i = 0; i < series.Count - 1; i++)
        {
            if (band(series[i]) != band(series[i + 1]))
            {
                return series[i].ComputedAt;
            }
        }

        return null;
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Both regime axes in one shape (FR-016). Axes are never merged into a single label.</summary>
public sealed record RegimeStateDto(
    DateTimeOffset? AsOf,
    VolatilityAxisDto Volatility,
    RatesAxisDto Rates)
{
    public static RegimeStateDto Empty { get; } = new(
        null, VolatilityAxisDto.Unavailable, RatesAxisDto.Unavailable);
}

public sealed record VolatilityAxisDto(
    bool Available,
    string? Regime,
    decimal? VixLevel,
    decimal? VixSma,
    string? Trend,
    DateTimeOffset? LastChange)
{
    public static VolatilityAxisDto Unavailable { get; } = new(false, null, null, null, null, null);
}

public sealed record RatesAxisDto(
    bool Available,
    string? Regime,
    decimal? Dgs10,
    decimal? Dgs2,
    decimal? Spread,
    bool RecessionWarning,
    string? GrowthValueTilt,
    DateTimeOffset? LastChange)
{
    public static RatesAxisDto Unavailable { get; } = new(false, null, null, null, null, false, null, null);
}
