namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Pure absolute/benchmark/excess return math (FR-005), net-of-cost/tax (FR-007b), and the
/// not-evaluable path (R6 — proxy-ticker fallback is resolved by the caller before this is
/// invoked; the calculator only ever sees the ticker actually used). No I/O — SC-001 determinism.
/// </summary>
public class ThesisPerformanceCalculator : IThesisPerformanceCalculator
{
    private const decimal BpsDivisor = 10_000m;

    public ThesisPerformanceResult Calculate(ThesisPerformanceInput input)
    {
        if (!IsQuotable(input.FromSubjectPrice) || !IsQuotable(input.ToSubjectPrice) ||
            !IsQuotable(input.FromBenchmarkPrice) || !IsQuotable(input.ToBenchmarkPrice))
        {
            return NotEvaluable(input, "No quotable price for the subject ticker (or its proxy) or the benchmark at one of the two events.");
        }

        var absoluteReturnPct = PercentChange(input.FromSubjectPrice!.Value, input.ToSubjectPrice!.Value);
        var benchmarkReturnPct = PercentChange(input.FromBenchmarkPrice!.Value, input.ToBenchmarkPrice!.Value);
        var excessReturnPct = absoluteReturnPct - benchmarkReturnPct;

        var netAbsoluteReturnPct = ApplyFriction(absoluteReturnPct, input.FromTimestamp, input.ToTimestamp, input.Friction);
        var netExcessReturnPct = netAbsoluteReturnPct - benchmarkReturnPct;

        return new ThesisPerformanceResult(
            input.SubjectId,
            input.FromEvent,
            input.ToEvent,
            absoluteReturnPct,
            benchmarkReturnPct,
            excessReturnPct,
            netAbsoluteReturnPct,
            netExcessReturnPct,
            IsEvaluable: true,
            ExclusionReason: null,
            input.PriceSourceUsed);
    }

    private static bool IsQuotable(decimal? price) => price is > 0m;

    private static decimal PercentChange(decimal from, decimal to) => (to - from) / from * 100m;

    /// <summary>
    /// Round-trip cost (bps) is subtracted from gross return unconditionally; tax applies only to
    /// the gain portion (no tax drag on losses), at the short- or long-term rate per holding period.
    /// </summary>
    private static decimal ApplyFriction(
        decimal grossReturnPct,
        DateTimeOffset from,
        DateTimeOffset to,
        FrictionConfig friction)
    {
        var afterCostPct = grossReturnPct - friction.PerTradeCostBps / BpsDivisor * 100m;

        if (afterCostPct <= 0m)
        {
            return afterCostPct;
        }

        var holdingDays = (to - from).TotalDays;
        var taxRate = holdingDays >= friction.ShortLongBoundaryDays
            ? friction.LongTermTaxRate
            : friction.ShortTermTaxRate;

        return afterCostPct * (1m - taxRate);
    }

    private static ThesisPerformanceResult NotEvaluable(ThesisPerformanceInput input, string reason)
        => new(
            input.SubjectId,
            input.FromEvent,
            input.ToEvent,
            AbsoluteReturnPct: null,
            BenchmarkReturnPct: null,
            ExcessReturnPct: null,
            NetAbsoluteReturnPct: null,
            NetExcessReturnPct: null,
            IsEvaluable: false,
            ExclusionReason: reason,
            input.PriceSourceUsed);
}
