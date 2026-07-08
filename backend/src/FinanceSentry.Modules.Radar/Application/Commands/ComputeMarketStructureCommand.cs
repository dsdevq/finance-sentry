namespace FinanceSentry.Modules.Radar.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using Microsoft.Extensions.Options;

/// <summary>
/// Runs the market-structure calculator over the universe and emits signals to the shared log. In
/// <see cref="ScannerMode.LogOnly"/> (default) zero Alerts are raised; only in
/// <see cref="ScannerMode.Alerting"/> does a held ticker's unusual move at/above the alert bar raise
/// an <c>AlertType.MarketStructure</c> Alert (FR-010/015).
/// </summary>
public sealed record ComputeMarketStructureCommand : ICommand<ComputeRunSummary>;

public sealed class ComputeMarketStructureCommandHandler(
    IStructureQueryService structure,
    IRadarSignalWriter signals,
    IBankingTotalsReader bankingTotals,
    IBrokerageHoldingsReader brokerageReader,
    IAlertGeneratorService alerts,
    IOptions<RadarOptions> options)
    : ICommandHandler<ComputeMarketStructureCommand, ComputeRunSummary>
{
    private const string EquityInstrumentType = "STK";
    private const int RotationWindow = StructureWindows.Quarter;

    private readonly RadarOptions _options = options.Value;

    public async Task<ComputeRunSummary> Handle(
        ComputeMarketStructureCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var counts = new Dictionary<string, int>();

        // Map each held equity ticker → owning users (for holder-scoped signals + alerts).
        var holdersByTicker = await BuildHoldersMapAsync(cancellationToken);

        var structures = await structure.GetStructuresAsync(null, cancellationToken);

        // ── breadth (info, every run) ────────────────────────────────────────
        var breadth = await structure.GetBreadthAsync(cancellationToken);
        await signals.AppendSignalAsync(new RadarSignalRequest(
            RadarScanners.MarketStructure,
            RadarSignalTypes.Breadth,
            SignalSeverity.Info,
            RadarSubjectTypes.Universe,
            "universe",
            null,
            $"{RadarScanners.MarketStructure}:{RadarSignalTypes.Breadth}:universe:{today:yyyy-MM-dd}",
            new Dictionary<string, object>
            {
                ["pctAboveMa20"] = breadth.PctAboveMa20 as object ?? string.Empty,
                ["pctAboveMa50"] = breadth.PctAboveMa50 as object ?? string.Empty,
                ["pctAboveMa200"] = breadth.PctAboveMa200 as object ?? string.Empty,
                ["evaluated"] = breadth.Evaluated,
            }), cancellationToken);
        Bump(counts, RadarSignalTypes.Breadth);

        // ── rotation_shift (notable) ─────────────────────────────────────────
        var rotation = await structure.GetSectorRotationAsync(cancellationToken);
        foreach (var row in rotation.Where(r => r.Window == RotationWindow && r.RankDelta is not null))
        {
            if (Math.Abs(row.RankDelta!.Value) < _options.RotationRankDeltaThreshold)
            {
                continue;
            }

            await signals.AppendSignalAsync(new RadarSignalRequest(
                RadarScanners.MarketStructure,
                RadarSignalTypes.RotationShift,
                SignalSeverity.Notable,
                RadarSubjectTypes.Sector,
                row.Sector,
                null,
                $"{RadarScanners.MarketStructure}:{RadarSignalTypes.RotationShift}:{row.Sector}:{row.Window}:{today:yyyy-MM-dd}",
                new Dictionary<string, object>
                {
                    ["sector"] = row.Sector,
                    ["window"] = row.Window,
                    ["rank"] = row.Rank,
                    ["rankDelta"] = row.RankDelta.Value,
                }), cancellationToken);
            Bump(counts, RadarSignalTypes.RotationShift);
        }

        // ── held_sector_laggard (notable) ────────────────────────────────────
        var laggardSectors = rotation
            .Where(r => r.Window == RotationWindow && r.Rank >= _options.LaggardBottomQuartileRank)
            .Select(r => r.Sector)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ── per-ticker signals ───────────────────────────────────────────────
        var computed = 0;
        foreach (var s in structures)
        {
            computed++;
            var isHeld = holdersByTicker.TryGetValue(s.Ticker, out var holders);

            // extended (info, silent)
            if (s.ExtensionFromMa50 is not null && s.ExtensionFromMa50.Value >= _options.ExtensionThreshold)
            {
                await signals.AppendSignalAsync(new RadarSignalRequest(
                    RadarScanners.MarketStructure,
                    RadarSignalTypes.Extended,
                    SignalSeverity.Info,
                    RadarSubjectTypes.Ticker,
                    s.Ticker,
                    isHeld ? holders!.First() : null,
                    $"{RadarScanners.MarketStructure}:{RadarSignalTypes.Extended}:{s.Ticker}:{today:yyyy-MM-dd}",
                    new Dictionary<string, object>
                    {
                        ["ticker"] = s.Ticker,
                        ["extensionFromMa50"] = s.ExtensionFromMa50.Value,
                    }), cancellationToken);
                Bump(counts, RadarSignalTypes.Extended);
            }

            // unusual_move (notable; held + alert bar → Alert only in Alerting mode)
            if (s.TodayZScore is not null && Math.Abs(s.TodayZScore.Value) >= _options.UnusualMoveZScore)
            {
                await signals.AppendSignalAsync(new RadarSignalRequest(
                    RadarScanners.MarketStructure,
                    RadarSignalTypes.UnusualMove,
                    SignalSeverity.Notable,
                    RadarSubjectTypes.Ticker,
                    s.Ticker,
                    isHeld ? holders!.First() : null,
                    $"{RadarScanners.MarketStructure}:{RadarSignalTypes.UnusualMove}:{s.Ticker}:{today:yyyy-MM-dd}",
                    new Dictionary<string, object>
                    {
                        ["ticker"] = s.Ticker,
                        ["zScore"] = s.TodayZScore.Value,
                        ["vol63"] = s.Vol63 as object ?? string.Empty,
                        ["volumeRatio"] = s.VolumeRatio as object ?? string.Empty,
                    }), cancellationToken);
                Bump(counts, RadarSignalTypes.UnusualMove);

                if (isHeld &&
                    _options.ScannerMode == ScannerMode.Alerting &&
                    Math.Abs(s.TodayZScore.Value) >= _options.UnusualMoveAlertZScore)
                {
                    foreach (var userId in holders!)
                    {
                        await alerts.GenerateMarketStructureAlertAsync(
                            userId,
                            DeterministicReferenceId(s.Ticker),
                            s.Ticker,
                            $"moved {s.TodayZScore.Value:0.0}σ vs its 63-day volatility",
                            cancellationToken);
                    }
                }
            }

            // held_sector_laggard (notable): held ticker whose sector is a laggard
            if (isHeld && laggardSectors.Count > 0 && laggardSectors.Contains(s.Ticker))
            {
                await signals.AppendSignalAsync(new RadarSignalRequest(
                    RadarScanners.MarketStructure,
                    RadarSignalTypes.HeldSectorLaggard,
                    SignalSeverity.Notable,
                    RadarSubjectTypes.Ticker,
                    s.Ticker,
                    holders!.First(),
                    $"{RadarScanners.MarketStructure}:{RadarSignalTypes.HeldSectorLaggard}:{s.Ticker}:{today:yyyy-MM-dd}",
                    new Dictionary<string, object> { ["ticker"] = s.Ticker }), cancellationToken);
                Bump(counts, RadarSignalTypes.HeldSectorLaggard);
            }
        }

        return new ComputeRunSummary(computed, counts, 0);
    }

    private async Task<Dictionary<string, List<Guid>>> BuildHoldersMapAsync(CancellationToken ct)
    {
        var map = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        var userIds = await bankingTotals.GetActiveUserIdsAsync(ct);
        foreach (var userId in userIds)
        {
            var holdings = await brokerageReader.GetHoldingsAsync(userId, ct);
            foreach (var h in holdings.Where(h =>
                         string.Equals(h.InstrumentType, EquityInstrumentType, StringComparison.OrdinalIgnoreCase)))
            {
                var ticker = h.Symbol.Trim().ToUpperInvariant();
                if (!map.TryGetValue(ticker, out var list))
                {
                    list = [];
                    map[ticker] = list;
                }

                if (!list.Contains(userId))
                {
                    list.Add(userId);
                }
            }
        }

        return map;
    }

    // Stable per-ticker reference id so alert dedup/resolve is deterministic across runs.
    private static Guid DeterministicReferenceId(string ticker)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"market_structure:{ticker.ToUpperInvariant()}"));
        return new Guid(bytes);
    }

    private static void Bump(Dictionary<string, int> counts, string key)
        => counts[key] = counts.TryGetValue(key, out var c) ? c + 1 : 1;
}
