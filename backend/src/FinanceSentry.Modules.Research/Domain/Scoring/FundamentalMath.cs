namespace FinanceSentry.Modules.Research.Domain.Scoring;

using FinanceSentry.Modules.Research.Domain.ThesisMonitor;

/// <summary>
/// Shared EDGAR-concept mapping + pure ratio math for anything that evaluates fundamentals
/// (017's <see cref="ThesisBreakEvaluator"/> and 019's <c>FundamentalsScorer</c>). One concept
/// table, one place division-by-zero is guarded — no silent duplication of either (per 019 plan).
/// </summary>
public static class FundamentalMath
{
    public const string FiscalYearPeriod = "FY";

    public static readonly IReadOnlyDictionary<string, string> RawConceptByMetric = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ThesisMetric.Revenue] = "Revenue",
        [ThesisMetric.NetIncome] = "NetIncome",
        [ThesisMetric.DilutedEps] = "DilutedEPS",
    };

    public static readonly IReadOnlyDictionary<string, (string Numerator, string Denominator)> MarginConceptsByMetric =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            [ThesisMetric.GrossMargin] = ("GrossProfit", "Revenue"),
            [ThesisMetric.OperatingMargin] = ("OperatingIncome", "Revenue"),
            [ThesisMetric.NetMargin] = ("NetIncome", "Revenue"),
        };

    public static readonly IReadOnlyDictionary<string, string> YoyConceptByMetric = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ThesisMetric.RevenueYoy] = "Revenue",
        [ThesisMetric.NetIncomeYoy] = "NetIncome",
        [ThesisMetric.OperatingIncomeYoy] = "OperatingIncome",
        [ThesisMetric.EpsYoy] = "DilutedEPS",
    };

    /// <summary>Newest-first, one fact per period-end, for the given concept + period cadence.</summary>
    public static IReadOnlyList<FundamentalFact> SelectPeriods(
        IReadOnlyList<FundamentalFact> facts, string concept, ThesisPeriodType periodType)
        => facts
            .Where(f => string.Equals(f.Concept, concept, StringComparison.Ordinal) && MatchesPeriodType(f, periodType))
            .GroupBy(f => f.PeriodEnd)
            .Select(g => g.First())
            .OrderByDescending(f => f.PeriodEnd)
            .ToList();

    public static bool MatchesPeriodType(FundamentalFact fact, ThesisPeriodType periodType) => periodType switch
    {
        ThesisPeriodType.Annual => string.Equals(fact.FiscalPeriod, FiscalYearPeriod, StringComparison.OrdinalIgnoreCase),
        _ => fact.FiscalPeriod is not null && !string.Equals(fact.FiscalPeriod, FiscalYearPeriod, StringComparison.OrdinalIgnoreCase),
    };

    public static string Label(FundamentalFact fact)
        => fact.FiscalYear is { } year
            ? $"{year}{fact.FiscalPeriod ?? FiscalYearPeriod}"
            : fact.PeriodEnd.ToString("yyyy-MM-dd");

    /// <summary>
    /// A single period's ratio (e.g. gross margin) — null denominator/zero denominator both
    /// surface as "not evaluable" to the caller rather than throwing or defaulting to 0.
    /// </summary>
    public static decimal? SafeRatio(decimal numerator, decimal denominator)
        => denominator == 0 ? null : numerator / denominator;

    /// <summary>Year-over-year change for a same-fiscal-period pair; null when the prior value is zero.</summary>
    public static decimal? SafeYoy(decimal current, decimal prior)
        => prior == 0 ? null : (current - prior) / prior;

    /// <summary>
    /// Latest quarterly year-over-year change of a raw concept (matches the same fiscal period one
    /// year earlier), or null when not evaluable. Used by 019's fundamentals scorer.
    /// </summary>
    public static decimal? LatestYoy(IReadOnlyList<FundamentalFact> facts, string concept)
    {
        var periods = SelectPeriods(facts, concept, ThesisPeriodType.Quarter)
            .Where(f => f.FiscalYear is not null && f.FiscalPeriod is not null)
            .ToList();
        if (periods.Count == 0)
        {
            return null;
        }

        var byKey = periods
            .GroupBy(f => (f.FiscalYear!.Value, f.FiscalPeriod!))
            .ToDictionary(g => g.Key, g => g.First().Value);

        var latest = periods[0];
        var priorKey = (latest.FiscalYear!.Value - 1, latest.FiscalPeriod!);
        return byKey.TryGetValue(priorKey, out var prior) ? SafeYoy(latest.Value, prior) : null;
    }

    /// <summary>Latest quarterly margin (numerator/denominator), or null when not evaluable.</summary>
    public static decimal? LatestMargin(
        IReadOnlyList<FundamentalFact> facts, string numeratorConcept, string denominatorConcept)
    {
        var margins = MarginSeries(facts, numeratorConcept, denominatorConcept);
        return margins.Count == 0 ? null : margins[0].Margin;
    }

    /// <summary>
    /// Change in margin between the latest quarter and the quarter <paramref name="lookback"/> periods
    /// earlier (default 4 = one year). Null when fewer than <paramref name="lookback"/>+1 margins exist.
    /// </summary>
    public static decimal? MarginTrend(
        IReadOnlyList<FundamentalFact> facts, string numeratorConcept, string denominatorConcept, int lookback = 4)
    {
        var margins = MarginSeries(facts, numeratorConcept, denominatorConcept);
        return margins.Count <= lookback ? null : margins[0].Margin - margins[lookback].Margin;
    }

    private static IReadOnlyList<(DateOnly PeriodEnd, decimal Margin)> MarginSeries(
        IReadOnlyList<FundamentalFact> facts, string numeratorConcept, string denominatorConcept)
    {
        var numerators = SelectPeriods(facts, numeratorConcept, ThesisPeriodType.Quarter);
        var denominators = SelectPeriods(facts, denominatorConcept, ThesisPeriodType.Quarter)
            .ToDictionary(f => f.PeriodEnd, f => f.Value);

        var series = new List<(DateOnly, decimal)>();
        foreach (var num in numerators)
        {
            if (denominators.TryGetValue(num.PeriodEnd, out var denom))
            {
                var margin = SafeRatio(num.Value, denom);
                if (margin is not null)
                {
                    series.Add((num.PeriodEnd, margin.Value));
                }
            }
        }

        return series;
    }
}
