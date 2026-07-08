namespace FinanceSentry.Modules.Research.Tests.Opportunity;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Scoring;
using FluentAssertions;
using Xunit;

public sealed class FundamentalsScorerTests
{
    private static FundamentalFact Fact(
        string concept, decimal value, int fiscalYear, string fiscalPeriod, DateOnly periodEnd)
        => new("MU", concept, concept, "USD", value, periodEnd, fiscalPeriod, fiscalYear, "10-Q");

    [Fact]
    public void Score_ReturnsNull_WhenNoFactsAtAll()
    {
        var (score, revenueYoy, marginLatest, marginTrend, epsYoy, reasons) =
            FundamentalsScorer.Score([]);

        score.Should().BeNull();
        revenueYoy.Should().BeNull();
        marginLatest.Should().BeNull();
        marginTrend.Should().BeNull();
        epsYoy.Should().BeNull();
        reasons.Should().Contain("no_fundamentals_data");
    }

    [Fact]
    public void Score_IsPartial_WhenOnlyRevenueIsAvailable()
    {
        var facts = new List<FundamentalFact>
        {
            Fact("Revenue", 120m, 2026, "Q2", new DateOnly(2026, 5, 31)),
            Fact("Revenue", 100m, 2025, "Q2", new DateOnly(2025, 5, 31)),
        };

        var (score, revenueYoy, marginLatest, marginTrend, epsYoy, reasons) =
            FundamentalsScorer.Score(facts);

        score.Should().NotBeNull();
        revenueYoy.Should().Be(0.2m);
        marginLatest.Should().BeNull();
        marginTrend.Should().BeNull();
        epsYoy.Should().BeNull();
        reasons.Should().Contain("gross_margin_not_evaluable");
        reasons.Should().Contain("eps_yoy_not_evaluable");
    }

    [Fact]
    public void Score_GuardsDivideByZero_ForZeroRevenueDenominator()
    {
        var facts = new List<FundamentalFact>
        {
            Fact("GrossProfit", 30m, 2026, "Q2", new DateOnly(2026, 5, 31)),
            Fact("Revenue", 0m, 2026, "Q2", new DateOnly(2026, 5, 31)),
        };

        var (_, _, marginLatest, _, _, reasons) = FundamentalsScorer.Score(facts);

        marginLatest.Should().BeNull();
        reasons.Should().Contain("gross_margin_not_evaluable");
    }

    [Fact]
    public void Score_IsDeterministic_ForIdenticalInputs()
    {
        // Five quarters of margins: the spec's 4-quarter trend window (FR-004) compares the
        // latest margin against the one a year earlier — margins[0] vs margins[4].
        var facts = new List<FundamentalFact>
        {
            Fact("Revenue", 120m, 2026, "Q2", new DateOnly(2026, 5, 31)),
            Fact("Revenue", 110m, 2026, "Q1", new DateOnly(2026, 2, 28)),
            Fact("Revenue", 105m, 2025, "Q4", new DateOnly(2025, 11, 30)),
            Fact("Revenue", 102m, 2025, "Q3", new DateOnly(2025, 8, 31)),
            Fact("Revenue", 100m, 2025, "Q2", new DateOnly(2025, 5, 31)),
            Fact("GrossProfit", 60m, 2026, "Q2", new DateOnly(2026, 5, 31)),
            Fact("GrossProfit", 45m, 2026, "Q1", new DateOnly(2026, 2, 28)),
            Fact("GrossProfit", 44m, 2025, "Q4", new DateOnly(2025, 11, 30)),
            Fact("GrossProfit", 43m, 2025, "Q3", new DateOnly(2025, 8, 31)),
            Fact("GrossProfit", 42m, 2025, "Q2", new DateOnly(2025, 5, 31)),
            Fact("DilutedEPS", 2.4m, 2026, "Q2", new DateOnly(2026, 5, 31)),
            Fact("DilutedEPS", 2.0m, 2025, "Q2", new DateOnly(2025, 5, 31)),
        };

        var run1 = FundamentalsScorer.Score(facts);
        var run2 = FundamentalsScorer.Score(facts);

        run1.Score.Should().Be(run2.Score);
        run1.Score.Should().NotBeNull();
        run1.GrossMarginTrend.Should().NotBeNull();
    }
}
