namespace FinanceSentry.Modules.Research.Domain.Scoring;

using FinanceSentry.Core.Interfaces;

/// <summary>
/// Pure, deterministic 0-100 structure sub-score from an 018 <see cref="MarketStructureSnapshot"/>
/// (relative strength, extension from the 50-day MA, today's volume z-score). No EF, no HTTP, no
/// LLM. Returns null (never a faked 0/50/100) when there is no evaluable structure data at all.
/// </summary>
public static class StructureScorer
{
    private const decimal MinScore = 0m;
    private const decimal MaxScore = 100m;
    private const decimal Midpoint = 50m;

    // Every 25 percentage points of RS outperformance moves the RS component by 50 points.
    private const decimal RsScaleFactor = 200m;

    // An extension of ~5% above MA50 is treated as the sweet spot; further extension penalizes.
    private const decimal ExtensionSweetSpot = 0.05m;
    private const decimal ExtensionScaleFactor = 200m;
    private const decimal NegativeExtensionScaleFactor = 100m;

    // Each unit of z-score moves the component by 10 points off the midpoint.
    private const decimal ZScoreScaleFactor = 10m;

    // Sector rank 1..11 maps 100 down to 0; each rank position costs 10 points.
    private const decimal SectorRankStep = 10m;

    // Each rank position moved (delta) shifts the component by 5 points (falling rank penalizes).
    private const decimal SectorRankDeltaStep = 5m;

    // Each 1% below the 63-day high costs 4 points; ~25% below the high scores 0.
    private const decimal BreakoutScaleFactor = 400m;

    public static (int? Score, IReadOnlyList<string> NotEvaluableReasons) Score(MarketStructureSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return (null, ["no_structure_data"]);
        }

        var components = new List<decimal>();
        var reasons = new List<string>();

        var rsValues = snapshot.RsByWindow.Values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (rsValues.Count > 0)
        {
            var avgRs = rsValues.Average();
            components.Add(Clamp(Midpoint + (avgRs * RsScaleFactor)));
        }
        else
        {
            reasons.Add("no_rs_windows");
        }

        if (snapshot.ExtensionFromMa50 is { } extension)
        {
            var extensionScore = extension >= 0
                ? Clamp(MaxScore - ((extension - ExtensionSweetSpot) * ExtensionScaleFactor))
                : Clamp(Midpoint + (extension * NegativeExtensionScaleFactor));
            components.Add(extensionScore);
        }
        else
        {
            reasons.Add("no_extension_data");
        }

        if (snapshot.TodayZScore is { } zScore)
        {
            components.Add(Clamp(Midpoint + (zScore * ZScoreScaleFactor)));
        }
        else
        {
            reasons.Add("no_zscore_data");
        }

        // FR-003: sector rotation position — rank 1 of 11 scores high, laggards low; a rising
        // rank (negative delta = climbed) nudges up, a falling rank down.
        if (snapshot.SectorRank is { } rank)
        {
            var rankScore = Clamp(MaxScore - ((rank - 1) * SectorRankStep));
            if (snapshot.SectorRankDelta is { } delta)
            {
                rankScore = Clamp(rankScore - (delta * SectorRankDeltaStep));
            }

            components.Add(rankScore);
        }
        else
        {
            reasons.Add("no_sector_rank");
        }

        // FR-003: breakout state — at/near the 63-day high scores high, far below scores low.
        if (snapshot.DistanceFrom63dHigh is { } distance)
        {
            components.Add(Clamp(MaxScore + (distance * BreakoutScaleFactor)));
        }
        else
        {
            reasons.Add("no_breakout_data");
        }

        if (snapshot.Stale)
        {
            reasons.Add("stale_structure_data");
        }

        if (components.Count == 0)
        {
            return (null, reasons);
        }

        var score = (int)Math.Round(components.Average(), MidpointRounding.AwayFromZero);
        return (score, reasons);
    }

    private static decimal Clamp(decimal value) => Math.Clamp(value, MinScore, MaxScore);
}
