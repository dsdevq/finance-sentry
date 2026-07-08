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
