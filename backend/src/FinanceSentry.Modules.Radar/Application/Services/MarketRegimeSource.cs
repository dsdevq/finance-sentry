namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Domain.Regime;
using FinanceSentry.Modules.Radar.Domain.Repositories;

/// <summary>
/// <see cref="IMarketRegimeSource"/> impl (feature 021) — projects the latest persisted
/// <see cref="RegimeReading"/> to the Core cross-boundary snapshot (strings, no Radar enums), so the
/// Research 019 scorer can read regime context without referencing the Radar module.
/// </summary>
public sealed class MarketRegimeSource(IRegimeReadingRepository readings) : IMarketRegimeSource
{
    // Band changes are rare; a bounded window is enough to locate the last change per axis.
    private const int RecentScanLimit = 400;

    public async Task<MarketRegimeSnapshot?> GetLatestAsync(CancellationToken ct = default)
    {
        var latest = await readings.LatestAsync(ct);
        if (latest is null)
        {
            return null;
        }

        var recent = await readings.RecentAsync(RecentScanLimit, ct);

        return new MarketRegimeSnapshot(
            latest.ComputedAt,
            latest.VolatilityAvailable,
            latest.VolatilityRegime?.ToString(),
            latest.VixLevel,
            latest.VixTrend?.ToString(),
            latest.RatesAvailable,
            latest.RatesRegime?.ToString(),
            latest.Spread,
            latest.RecessionWarning,
            latest.GrowthValueTilt,
            latest.VolatilityAvailable
                ? LastChange(recent, r => r.VolatilityAvailable, r => r.VolatilityRegime?.ToString())
                : null,
            latest.RatesAvailable
                ? LastChange(recent, r => r.RatesAvailable, r => r.RatesRegime?.ToString())
                : null);
    }

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
