namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.Extensions.Options;

/// <summary>
/// Pure reads over persisted bars/signals — the shared compute path behind every read tool. Never
/// triggers ingestion (FR-011). Attaches the staleness flag (FR-017).
/// </summary>
public sealed class StructureQueryService(
    IDailyBarRepository bars,
    IRadarUniverseRepository universe,
    IRadarSignalRepository signals,
    IOptions<RadarOptions> options) : IStructureQueryService
{
    private const int RotationPrimaryWindow = StructureWindows.Quarter;
    private const int RotationPriorOffset = StructureWindows.Month; // rank delta vs 21 trading days prior
    private const int TopN = 3;

    private readonly RadarOptions _options = options.Value;

    public async Task<TickerStructure?> GetStructureAsync(string ticker, CancellationToken ct = default)
    {
        var normalized = ticker.Trim().ToUpperInvariant();
        var since = LookbackSince();

        var tickerBars = await bars.GetSinceAsync(normalized, since, ct);
        if (tickerBars.Count == 0)
        {
            return null;
        }

        var benchmarkBars = await bars.GetSinceAsync(_options.Benchmark, since, ct);
        var benchmarkReturns = MarketStructureCalculator.BenchmarkReturns(benchmarkBars);
        var stale = IsStale(tickerBars);

        return MarketStructureCalculator.Compute(normalized, tickerBars, benchmarkReturns, stale);
    }

    public async Task<IReadOnlyList<TickerStructure>> GetStructuresAsync(
        IReadOnlyList<string>? tickers, CancellationToken ct = default)
    {
        var since = LookbackSince();
        var benchmarkBars = await bars.GetSinceAsync(_options.Benchmark, since, ct);
        var benchmarkReturns = MarketStructureCalculator.BenchmarkReturns(benchmarkBars);

        var universeTickers = tickers is { Count: > 0 }
            ? tickers.Select(t => t.Trim().ToUpperInvariant()).ToList()
            : (await universe.ListActiveAsync(ct)).Select(m => m.Ticker).ToList();

        var results = new List<TickerStructure>();
        foreach (var ticker in universeTickers)
        {
            var tickerBars = await bars.GetSinceAsync(ticker, since, ct);
            if (tickerBars.Count == 0)
            {
                continue;
            }

            results.Add(MarketStructureCalculator.Compute(
                ticker, tickerBars, benchmarkReturns, IsStale(tickerBars)));
        }

        return results
            .OrderByDescending(s => s.RsByWindow.TryGetValue(StructureWindows.Quarter, out var rs) && rs.HasValue)
            .ThenByDescending(s => s.RsByWindow.TryGetValue(StructureWindows.Quarter, out var rs) ? rs ?? decimal.MinValue : decimal.MinValue)
            .ToList();
    }

    public async Task<IReadOnlyList<SectorRotationRow>> GetSectorRotationAsync(CancellationToken ct = default)
    {
        var since = LookbackSince();
        var benchmarkBars = await bars.GetSinceAsync(_options.Benchmark, since, ct);

        var sectorBars = new Dictionary<string, IReadOnlyList<DailyBar>>();
        foreach (var ticker in _options.SectorTickers)
        {
            var b = await bars.GetSinceAsync(ticker, since, ct);
            if (b.Count > 0)
            {
                sectorBars[ticker] = b;
            }
        }

        var rows = new List<SectorRotationRow>();
        foreach (var window in StructureWindows.All)
        {
            var currentRs = ComputeSectorRs(sectorBars, benchmarkBars, window, truncate: 0);
            var priorRs = ComputeSectorRs(sectorBars, benchmarkBars, window, truncate: RotationPriorOffset);
            var priorRanks = SectorRotation.Rank(priorRs);
            rows.AddRange(SectorRotation.BuildRows(window, currentRs, priorRanks));
        }

        return rows;
    }

    public async Task<BreadthResult> GetBreadthAsync(CancellationToken ct = default)
    {
        var since = LookbackSince();
        var members = await universe.ListActiveAsync(ct);

        var states = new List<Breadth.TickerMaState>();
        foreach (var member in members)
        {
            var b = await bars.GetSinceAsync(member.Ticker, since, ct);
            if (b.Count == 0)
            {
                continue;
            }

            var closes = b.Select(x => x.AdjClose).ToArray();
            states.Add(new Breadth.TickerMaState(
                closes[^1],
                MovingAverages.Sma(closes, 20),
                MovingAverages.Sma(closes, 50),
                MovingAverages.Sma(closes, 200)));
        }

        return Breadth.Compute(states);
    }

    public async Task<RadarSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var rotation = await GetSectorRotationAsync(ct);
        var primary = rotation.Where(r => r.Window == RotationPrimaryWindow).OrderBy(r => r.Rank).ToList();
        var leaders = primary.Take(TopN).ToList();
        var laggards = primary.OrderByDescending(r => r.Rank).Take(TopN).ToList();

        var breadth = await GetBreadthAsync(ct);

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var todaySignals = await signals.ListAsync(new SignalFilter(Since: todayStart), ct);
        var notable = todaySignals
            .Where(s => s.Severity != Core.Interfaces.SignalSeverity.Info)
            .Select(SignalMapper.ToDto)
            .ToList();

        var benchmarkLatest = await bars.GetLatestDateAsync(_options.Benchmark, ct);
        var stale = FreshnessEvaluator.IsStale(
            benchmarkLatest, DateOnly.FromDateTime(DateTime.UtcNow), _options.FreshnessMaxTradingDays);

        return new RadarSummary(leaders, laggards, breadth, notable, stale);
    }

    public async Task<IReadOnlyList<RadarSignalDto>> ListSignalsAsync(SignalFilter filter, CancellationToken ct = default)
    {
        var found = await signals.ListAsync(filter, ct);
        return found.Select(SignalMapper.ToDto).ToList();
    }

    private static IReadOnlyDictionary<string, decimal?> ComputeSectorRs(
        IReadOnlyDictionary<string, IReadOnlyList<DailyBar>> sectorBars,
        IReadOnlyList<DailyBar> benchmarkBars,
        int window,
        int truncate)
    {
        var benchCloses = Truncate(benchmarkBars.Select(b => b.AdjClose).ToArray(), truncate);
        var benchReturn = ReturnMath.Return(benchCloses, window);

        var rs = new Dictionary<string, decimal?>();
        foreach (var (ticker, b) in sectorBars)
        {
            var closes = Truncate(b.Select(x => x.AdjClose).ToArray(), truncate);
            var ret = ReturnMath.Return(closes, window);
            rs[ticker] = ReturnMath.RelativeStrength(ret, benchReturn);
        }

        return rs;
    }

    private static decimal[] Truncate(decimal[] closes, int drop)
        => drop <= 0 || drop >= closes.Length ? closes : closes[..^drop];

    private bool IsStale(IReadOnlyList<DailyBar> tickerBars)
    {
        var latest = tickerBars.Count > 0 ? tickerBars[^1].Date : (DateOnly?)null;
        return FreshnessEvaluator.IsStale(
            latest, DateOnly.FromDateTime(DateTime.UtcNow), _options.FreshnessMaxTradingDays);
    }

    private DateOnly LookbackSince()
        => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-(int)Math.Ceiling(_options.LookbackTradingDays * 1.5));
}
