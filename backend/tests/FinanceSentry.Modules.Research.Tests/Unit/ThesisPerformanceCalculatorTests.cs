namespace FinanceSentry.Modules.Research.Tests.Unit;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FluentAssertions;
using Xunit;

public class ThesisPerformanceCalculatorTests
{
    private static readonly DateTimeOffset FromTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ToTimestamp = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly FrictionConfig NoFriction = new()
    {
        PerTradeCostBps = 0m,
        ShortTermTaxRate = 0m,
        LongTermTaxRate = 0m,
    };

    private readonly ThesisPerformanceCalculator sut = new();

    private static ThesisPerformanceInput Input(
        decimal? fromSubject = 100m,
        decimal? fromBenchmark = 500m,
        decimal? toSubject = 120m,
        decimal? toBenchmark = 510m,
        FrictionConfig? friction = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
        => new(
            SubjectId: Guid.NewGuid(),
            FromEvent: ThesisEventType.Created,
            FromTimestamp: from ?? FromTimestamp,
            FromSubjectPrice: fromSubject,
            FromBenchmarkPrice: fromBenchmark,
            ToEvent: ThesisEventType.Snapshot,
            ToTimestamp: to ?? ToTimestamp,
            ToSubjectPrice: toSubject,
            ToBenchmarkPrice: toBenchmark,
            PriceSourceUsed: "MU",
            Friction: friction ?? NoFriction);

    [Fact]
    public void Calculate_ComputesAbsoluteBenchmarkAndExcessReturn()
    {
        var result = sut.Calculate(Input());

        result.IsEvaluable.Should().BeTrue();
        result.AbsoluteReturnPct.Should().Be(20m);
        result.BenchmarkReturnPct.Should().Be(2m);
        result.ExcessReturnPct.Should().Be(18m);
    }

    [Fact]
    public void Calculate_IsDeterministic_SameInputsProduceSameOutputs()
    {
        var input = Input();

        var first = sut.Calculate(input);
        var second = sut.Calculate(input);

        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void Calculate_NotEvaluable_WhenFromSubjectPriceIsNull()
    {
        var result = sut.Calculate(Input(fromSubject: null));

        result.IsEvaluable.Should().BeFalse();
        result.ExclusionReason.Should().NotBeNullOrEmpty();
        result.AbsoluteReturnPct.Should().BeNull();
        result.ExcessReturnPct.Should().BeNull();
    }

    [Fact]
    public void Calculate_NotEvaluable_WhenToSubjectPriceIsNull()
    {
        var result = sut.Calculate(Input(toSubject: null));

        result.IsEvaluable.Should().BeFalse();
    }

    [Fact]
    public void Calculate_NotEvaluable_WhenBenchmarkPriceMissing()
    {
        var result = sut.Calculate(Input(toBenchmark: null));

        result.IsEvaluable.Should().BeFalse();
    }

    [Fact]
    public void Calculate_NetReturn_SubtractsCostBpsAndAppliesShortTermTax_ForGainsHeldUnderBoundary()
    {
        var friction = new FrictionConfig
        {
            PerTradeCostBps = 100m, // 1%
            ShortTermTaxRate = 0.30m,
            LongTermTaxRate = 0.15m,
            ShortLongBoundaryDays = 365,
        };

        // Held from Jan 1 to Jun 1 (< 365 days) => short-term rate applies.
        var result = sut.Calculate(Input(friction: friction));

        // Gross 20% - 1% cost = 19% after cost; taxed at 30% on the gain => 19% * (1 - 0.30) = 13.3%
        result.NetAbsoluteReturnPct.Should().Be(19m * (1m - 0.30m));
        result.NetExcessReturnPct.Should().Be(result.NetAbsoluteReturnPct - result.BenchmarkReturnPct);
    }

    [Fact]
    public void Calculate_NetReturn_AppliesLongTermRate_ForGainsHeldAtOrOverBoundary()
    {
        var friction = new FrictionConfig
        {
            PerTradeCostBps = 0m,
            ShortTermTaxRate = 0.30m,
            LongTermTaxRate = 0.15m,
            ShortLongBoundaryDays = 30,
        };

        var result = sut.Calculate(Input(friction: friction, from: FromTimestamp, to: FromTimestamp.AddDays(400)));

        result.NetAbsoluteReturnPct.Should().Be(20m * (1m - 0.15m));
    }

    [Fact]
    public void Calculate_NetReturn_AppliesNoTax_WhenAfterCostReturnIsNegative()
    {
        var friction = new FrictionConfig
        {
            PerTradeCostBps = 0m,
            ShortTermTaxRate = 0.30m,
            LongTermTaxRate = 0.15m,
            ShortLongBoundaryDays = 365,
        };

        // A loss (to < from): no tax drag on losses per R3.
        var result = sut.Calculate(Input(toSubject: 80m, friction: friction));

        result.AbsoluteReturnPct.Should().Be(-20m);
        result.NetAbsoluteReturnPct.Should().Be(-20m);
    }
}
