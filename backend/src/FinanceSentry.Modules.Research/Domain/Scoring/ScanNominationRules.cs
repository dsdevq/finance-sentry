namespace FinanceSentry.Modules.Research.Domain.Scoring;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Pure, deterministic FR-008 nomination rules over the active universe (019 US2):
/// (a) top-quartile RS members of top-N rotating sectors, (b) top-decile RS overall,
/// (c) at the 63-day high on above-average volume. ETF lenses and stale snapshots are
/// never nominated. Reason strings are stable so re-nominations dedup instead of
/// accumulating near-duplicates on the candidate.
/// </summary>
public static class ScanNominationRules
{
    /// <summary>Window (bars) whose relative strength drives rules (a) and (b).</summary>
    public const int RsWindowBars = 63;

    public const string TopQuartileRotatingSectorReason = "scan: top-quartile RS in top rotating sector";
    public const string TopDecileRsReason = "scan: top-decile RS";
    public const string BreakoutReason = "scan: 63d-high breakout on above-average volume";

    public static IReadOnlyList<ScanNomination> Evaluate(
        IReadOnlyList<UniverseStructureEntry> universe, OpportunityOptions options)
    {
        var eligible = universe
            .Where(e => !e.IsEtfLens && !e.Snapshot.Stale)
            .ToList();
        if (eligible.Count == 0)
        {
            return [];
        }

        var rsByTicker = eligible
            .Select(e => (e.Ticker, Rs: RsAtWindow(e.Snapshot)))
            .Where(x => x.Rs is not null)
            .ToDictionary(x => x.Ticker, x => x.Rs!.Value, StringComparer.OrdinalIgnoreCase);
        var percentiles = PercentileRanks(rsByTicker);

        var nominations = new List<ScanNomination>();
        foreach (var entry in eligible)
        {
            var reasons = new List<string>();
            var percentile = percentiles.TryGetValue(entry.Ticker, out var p) ? p : (decimal?)null;

            if (percentile >= options.ScanTopQuartileRsPercentile
                && entry.Snapshot.SectorRank is { } rank
                && rank <= options.ScanTopRotatingSectors)
            {
                reasons.Add(TopQuartileRotatingSectorReason);
            }

            if (percentile >= options.ScanTopDecileRsPercentile)
            {
                reasons.Add(TopDecileRsReason);
            }

            if (entry.Snapshot.DistanceFrom63dHigh == 0m
                && entry.Snapshot.VolumeRatio >= options.ScanBreakoutVolumeRatioMin)
            {
                reasons.Add(BreakoutReason);
            }

            if (reasons.Count > 0)
            {
                nominations.Add(new ScanNomination(entry.Ticker, reasons, percentile));
            }
        }

        return nominations
            .OrderByDescending(n => n.RsPercentile ?? -1m)
            .ThenBy(n => n.Ticker, StringComparer.Ordinal)
            .ToList();
    }

    private static decimal? RsAtWindow(MarketStructureSnapshot snapshot)
        => snapshot.RsByWindow.TryGetValue(RsWindowBars, out var rs) ? rs : null;

    /// <summary>
    /// Inclusive percentile rank per ticker: share of universe values at or below the ticker's RS,
    /// scaled 0-100. A one-member universe ranks 100 (trivially at the top of what exists).
    /// </summary>
    private static Dictionary<string, decimal> PercentileRanks(Dictionary<string, decimal> rsByTicker)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (rsByTicker.Count == 0)
        {
            return result;
        }

        var values = rsByTicker.Values.OrderBy(v => v).ToList();
        foreach (var (ticker, rs) in rsByTicker)
        {
            var atOrBelow = values.Count(v => v <= rs);
            result[ticker] = Math.Round(100m * atOrBelow / values.Count, 2);
        }

        return result;
    }
}

public sealed record ScanNomination(string Ticker, IReadOnlyList<string> Reasons, decimal? RsPercentile);
