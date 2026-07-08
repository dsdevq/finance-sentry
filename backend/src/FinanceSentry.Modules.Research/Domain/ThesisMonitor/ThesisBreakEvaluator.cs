namespace FinanceSentry.Modules.Research.Domain.ThesisMonitor;

using FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Pure, deterministic evaluation of a single <see cref="ThesisInvalidationTrigger"/> against a
/// target ticker's fundamentals and/or price history. No EF, no HTTP, no LLM — the spine of the
/// thesis-break monitor (FR-006/FR-007/SC-001/SC-002).
/// </summary>
public static class ThesisBreakEvaluator
{
    private const string FiscalYearPeriod = "FY";

    private static readonly IReadOnlyDictionary<string, string> RawConceptByMetric = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ThesisMetric.Revenue] = "Revenue",
        [ThesisMetric.NetIncome] = "NetIncome",
        [ThesisMetric.DilutedEps] = "DilutedEPS",
    };

    private static readonly IReadOnlyDictionary<string, (string Numerator, string Denominator)> MarginConceptsByMetric =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            [ThesisMetric.GrossMargin] = ("GrossProfit", "Revenue"),
            [ThesisMetric.OperatingMargin] = ("OperatingIncome", "Revenue"),
            [ThesisMetric.NetMargin] = ("NetIncome", "Revenue"),
        };

    private static readonly IReadOnlyDictionary<string, string> YoyConceptByMetric = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ThesisMetric.RevenueYoy] = "Revenue",
        [ThesisMetric.NetIncomeYoy] = "NetIncome",
        [ThesisMetric.OperatingIncomeYoy] = "OperatingIncome",
        [ThesisMetric.EpsYoy] = "DilutedEPS",
    };

    public static TriggerVerdict Evaluate(
        ThesisInvalidationTrigger trigger,
        DateTimeOffset thesisCreatedAt,
        IReadOnlyList<FundamentalFact> fundamentals,
        IReadOnlyList<DailyClose> dailyCloses)
    {
        if (!ThesisMetric.Contains(trigger.Metric))
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.UnsupportedMetric);
        }

        return ThesisMetric.IsPriceMetric(trigger.Metric)
            ? EvaluatePrice(trigger, thesisCreatedAt, dailyCloses)
            : EvaluateFundamentals(trigger, fundamentals);
    }

    private static TriggerVerdict EvaluateFundamentals(
        ThesisInvalidationTrigger trigger, IReadOnlyList<FundamentalFact> fundamentals)
    {
        if (RawConceptByMetric.TryGetValue(trigger.Metric, out var rawConcept))
        {
            return EvaluateRaw(trigger, fundamentals, rawConcept);
        }

        if (MarginConceptsByMetric.TryGetValue(trigger.Metric, out var marginConcepts))
        {
            return EvaluateMargin(trigger, fundamentals, marginConcepts.Numerator, marginConcepts.Denominator);
        }

        if (YoyConceptByMetric.TryGetValue(trigger.Metric, out var yoyConcept))
        {
            return EvaluateYoy(trigger, fundamentals, yoyConcept);
        }

        return new TriggerVerdict.NonEvaluable(NonEvaluableReason.UnsupportedMetric);
    }

    private static TriggerVerdict EvaluateRaw(
        ThesisInvalidationTrigger trigger, IReadOnlyList<FundamentalFact> fundamentals, string concept)
    {
        var periods = SelectPeriods(fundamentals, concept, trigger.PeriodType);
        if (periods.Count == 0)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.NoFundamentals);
        }

        if (periods.Count < trigger.ConsecutivePeriods)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.InsufficientPeriods);
        }

        var window = periods.Take(trigger.ConsecutivePeriods).ToList();
        var values = window.Select(f => f.Value).ToArray();
        var labels = window.Select(Label).ToArray();

        return BuildVerdict(trigger, values, labels);
    }

    private static TriggerVerdict EvaluateMargin(
        ThesisInvalidationTrigger trigger,
        IReadOnlyList<FundamentalFact> fundamentals,
        string numeratorConcept,
        string denominatorConcept)
    {
        var numeratorPeriods = SelectPeriods(fundamentals, numeratorConcept, trigger.PeriodType);
        var denominatorPeriods = SelectPeriods(fundamentals, denominatorConcept, trigger.PeriodType)
            .ToDictionary(f => f.PeriodEnd);

        if (numeratorPeriods.Count == 0 || denominatorPeriods.Count == 0)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.NoFundamentals);
        }

        var values = new List<decimal>();
        var labels = new List<string>();

        foreach (var fact in numeratorPeriods)
        {
            if (values.Count == trigger.ConsecutivePeriods)
            {
                break;
            }

            if (!denominatorPeriods.TryGetValue(fact.PeriodEnd, out var denominatorFact))
            {
                continue;
            }

            if (denominatorFact.Value == 0)
            {
                return new TriggerVerdict.NonEvaluable(NonEvaluableReason.DivideByZero);
            }

            values.Add(fact.Value / denominatorFact.Value);
            labels.Add(Label(fact));
        }

        if (values.Count < trigger.ConsecutivePeriods)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.InsufficientPeriods);
        }

        return BuildVerdict(trigger, [.. values], [.. labels]);
    }

    private static TriggerVerdict EvaluateYoy(
        ThesisInvalidationTrigger trigger, IReadOnlyList<FundamentalFact> fundamentals, string concept)
    {
        var periods = SelectPeriods(fundamentals, concept, trigger.PeriodType);
        if (periods.Count == 0)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.NoFundamentals);
        }

        var byFiscalKey = periods
            .Where(f => f.FiscalYear is not null)
            .GroupBy(f => (f.FiscalYear!.Value, FiscalPeriod: f.FiscalPeriod ?? FiscalYearPeriod))
            .ToDictionary(g => g.Key, g => g.First());

        var values = new List<decimal>();
        var labels = new List<string>();

        foreach (var fact in periods)
        {
            if (values.Count == trigger.ConsecutivePeriods)
            {
                break;
            }

            if (fact.FiscalYear is null)
            {
                continue;
            }

            var priorKey = (fact.FiscalYear.Value - 1, fact.FiscalPeriod ?? FiscalYearPeriod);
            if (!byFiscalKey.TryGetValue(priorKey, out var prior))
            {
                continue;
            }

            if (prior.Value == 0)
            {
                return new TriggerVerdict.NonEvaluable(NonEvaluableReason.DivideByZero);
            }

            values.Add((fact.Value - prior.Value) / prior.Value);
            labels.Add(Label(fact));
        }

        if (values.Count < trigger.ConsecutivePeriods)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.InsufficientPeriods);
        }

        return BuildVerdict(trigger, [.. values], [.. labels]);
    }

    private static TriggerVerdict EvaluatePrice(
        ThesisInvalidationTrigger trigger, DateTimeOffset thesisCreatedAt, IReadOnlyList<DailyClose> dailyCloses)
    {
        var since = DateOnly.FromDateTime(thesisCreatedAt.UtcDateTime);
        var closes = dailyCloses.Where(c => c.Date >= since).OrderBy(c => c.Date).ToList();

        if (closes.Count == 0)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.NoPriceHistory);
        }

        if (closes.Count < trigger.ConsecutivePeriods)
        {
            return new TriggerVerdict.NonEvaluable(NonEvaluableReason.InsufficientPeriods);
        }

        var trailing = closes.TakeLast(trigger.ConsecutivePeriods).ToList();
        var values = new decimal[trailing.Count];
        var labels = new string[trailing.Count];

        for (var i = 0; i < trailing.Count; i++)
        {
            var day = trailing[i];
            var upToDay = closes.Where(c => c.Date <= day.Date).ToList();
            var baseline = trigger.Metric == ThesisMetric.PriceDrawdown
                ? upToDay.Max(c => c.Close)
                : upToDay[0].Close;

            if (baseline == 0)
            {
                return new TriggerVerdict.NonEvaluable(NonEvaluableReason.DivideByZero);
            }

            values[i] = trigger.Metric == ThesisMetric.PriceDrawdown
                ? (baseline - day.Close) / baseline
                : (day.Close - baseline) / baseline;
            labels[i] = day.Date.ToString("yyyy-MM-dd");
        }

        return BuildVerdict(trigger, values, labels);
    }

    private static TriggerVerdict BuildVerdict(
        ThesisInvalidationTrigger trigger, decimal[] values, string[] labels)
    {
        var allBreach = values.All(v => Breaches(v, trigger.Direction, trigger.Threshold));

        return allBreach
            ? new TriggerVerdict.Breached(trigger.Metric, values, labels, trigger.Threshold, trigger.Direction)
            : new TriggerVerdict.Held();
    }

    private static bool Breaches(decimal value, string direction, decimal threshold) => direction switch
    {
        "lessThan" => value < threshold,
        "greaterThan" => value > threshold,
        _ => false,
    };

    private static IReadOnlyList<FundamentalFact> SelectPeriods(
        IReadOnlyList<FundamentalFact> facts, string concept, ThesisPeriodType periodType)
        => facts
            .Where(f => string.Equals(f.Concept, concept, StringComparison.Ordinal) && MatchesPeriodType(f, periodType))
            .GroupBy(f => f.PeriodEnd)
            .Select(g => g.First())
            .OrderByDescending(f => f.PeriodEnd)
            .ToList();

    private static bool MatchesPeriodType(FundamentalFact fact, ThesisPeriodType periodType) => periodType switch
    {
        ThesisPeriodType.Annual => string.Equals(fact.FiscalPeriod, FiscalYearPeriod, StringComparison.OrdinalIgnoreCase),
        _ => fact.FiscalPeriod is not null && !string.Equals(fact.FiscalPeriod, FiscalYearPeriod, StringComparison.OrdinalIgnoreCase),
    };

    private static string Label(FundamentalFact fact)
        => fact.FiscalYear is { } year
            ? $"{year}{fact.FiscalPeriod ?? FiscalYearPeriod}"
            : fact.PeriodEnd.ToString("yyyy-MM-dd");
}
