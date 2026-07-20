namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Radar.Domain.Repositories;

/// <summary>
/// <see cref="IMarketStructureReader"/> impl: projects Radar's internal <c>TickerStructure</c> into
/// the Core-facing <see cref="MarketStructureSnapshot"/> so other modules (019) can read structure
/// without a compile-time dependency on Radar. Enriches the projection with the FR-003 scoring
/// inputs: the ticker's affinity-assigned sector rank/delta and distance from the 63-day high.
/// </summary>
public sealed class MarketStructureReader(
    IStructureQueryService structureQueryService,
    IDailyBarRepository bars,
    IRadarUniverseRepository universe) : IMarketStructureReader
{
    private const int BreakoutWindowBars = StructureWindows.Quarter;
    private const int RotationWindow = StructureWindows.Quarter;

    public async Task<MarketStructureSnapshot?> GetStructureAsync(string ticker, CancellationToken ct = default)
    {
        var structure = await structureQueryService.GetStructureAsync(ticker, ct);
        if (structure is null)
        {
            return null;
        }

        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-BreakoutWindowBars * 2);
        var series = await bars.GetSinceAsync(ticker.Trim().ToUpperInvariant(), since, ct);

        var (sectorRank, sectorRankDelta) = await ResolveSectorRankAsync(ticker, series, since, ct);

        return new MarketStructureSnapshot(
            structure.Ticker,
            structure.RsByWindow,
            structure.ReturnByWindow,
            structure.ExtensionFromMa50,
            structure.TodayZScore,
            structure.VolumeRatio,
            structure.Ma50,
            structure.Ma200,
            structure.Stale,
            sectorRank,
            sectorRankDelta,
            DistanceFrom63dHigh(series));
    }

    public async Task<IReadOnlyList<UniverseStructureEntry>> GetUniverseStructuresAsync(CancellationToken ct = default)
    {
        var members = await universe.ListActiveAsync(ct);
        var entries = new List<UniverseStructureEntry>(members.Count);
        foreach (var member in members)
        {
            var snapshot = await GetStructureAsync(member.Ticker, ct);
            if (snapshot is not null)
            {
                var isEtfLens = member.Kind is UniverseKind.Benchmark or UniverseKind.Sector or UniverseKind.Industry;
                entries.Add(new UniverseStructureEntry(snapshot.Ticker, isEtfLens, snapshot));
            }
        }

        return entries;
    }

    public async Task<IReadOnlyList<PairwiseCorrelation>> GetPairwiseCorrelationsAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-BreakoutWindowBars * 2);
        var closesByTicker = new Dictionary<string, IReadOnlyDictionary<DateOnly, decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var ticker in tickers.Select(t => t.Trim().ToUpperInvariant()).Distinct())
        {
            var series = await bars.GetSinceAsync(ticker, since, ct);
            if (series.Count > 0)
            {
                closesByTicker[ticker] = series.ToDictionary(b => b.Date, b => b.AdjClose);
            }
        }

        var ordered = closesByTicker.Keys.OrderBy(t => t, StringComparer.Ordinal).ToList();
        var result = new List<PairwiseCorrelation>();
        for (var i = 0; i < ordered.Count; i++)
        {
            for (var j = i + 1; j < ordered.Count; j++)
            {
                var correlation = SectorAffinity.ReturnCorrelation(closesByTicker[ordered[i]], closesByTicker[ordered[j]]);
                if (correlation is not null)
                {
                    result.Add(new PairwiseCorrelation(ordered[i], ordered[j], Math.Round(correlation.Value, 4)));
                }
            }
        }

        return result;
    }

    /// <summary>Latest adjusted close vs the max close over the last 63 bars (0 = at/above the high).</summary>
    private static decimal? DistanceFrom63dHigh(IReadOnlyList<DailyBar> series)
    {
        if (series.Count == 0)
        {
            return null;
        }

        var window = series.TakeLast(BreakoutWindowBars).Select(b => b.AdjClose).Where(c => c > 0).ToList();
        if (window.Count == 0)
        {
            return null;
        }

        var high = window.Max();
        var latest = window[^1];
        return high > 0 ? latest / high - 1m : null;
    }

    private async Task<(int? Rank, int? RankDelta)> ResolveSectorRankAsync(
        string ticker, IReadOnlyList<DailyBar> series, DateOnly since, CancellationToken ct)
    {
        var upper = ticker.Trim().ToUpperInvariant();
        var members = await universe.ListActiveAsync(ct);
        var rotation = await structureQueryService.GetSectorRotationAsync(ct);
        var rows = rotation.Where(r => r.Window == RotationWindow).ToList();
        if (rows.Count == 0)
        {
            return (null, null);
        }

        // A sector ETF ranks as itself; anything else maps via return-correlation affinity.
        string? sector = members.Any(m =>
                m.Kind == UniverseKind.Sector && string.Equals(m.Ticker, upper, StringComparison.OrdinalIgnoreCase))
            ? upper
            : await BestAffinitySectorAsync(series, members, since, ct);

        if (sector is null)
        {
            return (null, null);
        }

        var row = rows.FirstOrDefault(r => string.Equals(r.Sector, sector, StringComparison.OrdinalIgnoreCase));
        return row is null ? (null, null) : (row.Rank, row.RankDelta);
    }

    private async Task<string?> BestAffinitySectorAsync(
        IReadOnlyList<DailyBar> series,
        IReadOnlyList<RadarUniverseMember> members,
        DateOnly since,
        CancellationToken ct)
    {
        if (series.Count == 0)
        {
            return null;
        }

        var sectorCloses = new Dictionary<string, IReadOnlyDictionary<DateOnly, decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var sector in members.Where(m => m.Kind == UniverseKind.Sector))
        {
            var sectorSeries = await bars.GetSinceAsync(sector.Ticker, since, ct);
            if (sectorSeries.Count > 0)
            {
                sectorCloses[sector.Ticker] = sectorSeries.ToDictionary(b => b.Date, b => b.AdjClose);
            }
        }

        return SectorAffinity.BestSector(series.ToDictionary(b => b.Date, b => b.AdjClose), sectorCloses);
    }
}
