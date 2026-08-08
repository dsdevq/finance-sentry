using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain.Regime;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.Regime;

public sealed class RegimeClassifierTests
{
    private static readonly RegimeOptions Options = new();

    // ── Volatility bands (defaults: <15 Calm, <20 Normal, <30 Stressed, >=30 Panic) ──
    [Theory]
    [InlineData(10.0, VolatilityRegime.Calm)]
    [InlineData(14.99, VolatilityRegime.Calm)]
    [InlineData(15.0, VolatilityRegime.Normal)]   // exact lower boundary is inclusive-of-next-band
    [InlineData(19.99, VolatilityRegime.Normal)]
    [InlineData(20.0, VolatilityRegime.Stressed)]
    [InlineData(29.99, VolatilityRegime.Stressed)]
    [InlineData(30.0, VolatilityRegime.Panic)]    // exact 30 is Panic (>=)
    [InlineData(55.0, VolatilityRegime.Panic)]
    public void ClassifyVolatilityBand_RespectsBoundaries(double vix, VolatilityRegime expected)
        => RegimeClassifier.ClassifyVolatilityBand((decimal)vix, Options).Should().Be(expected);

    [Fact]
    public void Sma_ReturnsNull_WhenTooFewCloses()
    {
        var closes = Enumerable.Repeat(20m, 19).ToList(); // window is 20
        RegimeClassifier.Sma(closes, Options.VixSmaWindow).Should().BeNull();
    }

    [Fact]
    public void Sma_AveragesLastWindowCloses()
    {
        // 21 closes: first is an outlier that must be excluded by the 20-window.
        var closes = new List<decimal> { 100m };
        closes.AddRange(Enumerable.Repeat(10m, 20));
        RegimeClassifier.Sma(closes, Options.VixSmaWindow).Should().Be(10m);
    }

    [Theory]
    [InlineData(11.0, 10.0, RegimeTrend.Rising)]   // >2% above SMA
    [InlineData(9.0, 10.0, RegimeTrend.Falling)]   // >2% below SMA
    [InlineData(10.1, 10.0, RegimeTrend.Flat)]     // within +/-2% band
    public void ClassifyTrend_UsesBandAroundSma(double latest, double sma, RegimeTrend expected)
        => RegimeClassifier.ClassifyTrend((decimal)latest, (decimal)sma, Options.VixTrendBand).Should().Be(expected);

    [Fact]
    public void ClassifyTrend_Unknown_WhenNoSma()
        => RegimeClassifier.ClassifyTrend(20m, null, Options.VixTrendBand).Should().Be(RegimeTrend.Unknown);

    [Fact]
    public void AssessVolatility_Null_WhenNoCloses()
        => RegimeClassifier.AssessVolatility([], Options).Should().BeNull();

    [Fact]
    public void AssessVolatility_UsesLatestCloseAsLevel()
    {
        var closes = new List<decimal> { 12m, 13m, 34m }; // latest = 34 -> Panic
        var result = RegimeClassifier.AssessVolatility(closes, Options);
        result.Should().NotBeNull();
        result!.Level.Should().Be(34m);
        result.Regime.Should().Be(VolatilityRegime.Panic);
        result.Sma.Should().BeNull(); // fewer than 20 closes
        result.Trend.Should().Be(RegimeTrend.Unknown);
    }

    // ── Rates bands (defaults: <0 Inverted, <0.5 Flat, <1.5 Normal, >=1.5 Steep) ──
    [Theory]
    [InlineData(3.71, 4.08, RatesRegime.Inverted, true)]  // spread -0.37
    [InlineData(4.00, 4.00, RatesRegime.Flat, false)]     // spread 0 exactly -> Flat (>=0)
    [InlineData(4.40, 4.00, RatesRegime.Flat, false)]     // spread 0.40
    [InlineData(4.60, 4.00, RatesRegime.Normal, false)]   // spread 0.60
    [InlineData(5.49, 4.00, RatesRegime.Normal, false)]   // spread 1.49 (just under Steep)
    [InlineData(6.00, 4.00, RatesRegime.Steep, false)]    // spread 2.00
    public void AssessRates_ClassifiesSpreadAndRecession(
        double dgs10, double dgs2, RatesRegime expected, bool recession)
    {
        var result = RegimeClassifier.AssessRates((decimal)dgs10, (decimal)dgs2, Options);
        result.Spread.Should().Be((decimal)dgs10 - (decimal)dgs2);
        result.Regime.Should().Be(expected);
        result.RecessionWarning.Should().Be(recession);
    }

    [Fact]
    public void AssessRates_ExactSteepBoundary_IsSteep()
    {
        // spread exactly 1.5 -> Steep (>= SpreadNormalMax)
        var result = RegimeClassifier.AssessRates(5.5m, 4.0m, Options);
        result.Spread.Should().Be(1.5m);
        result.Regime.Should().Be(RatesRegime.Steep);
    }

    [Theory]
    [InlineData(RatesRegime.Inverted, RegimeClassifier.TiltInverted)]
    [InlineData(RatesRegime.Flat, RegimeClassifier.TiltFlat)]
    [InlineData(RatesRegime.Normal, RegimeClassifier.TiltNormal)]
    [InlineData(RatesRegime.Steep, RegimeClassifier.TiltSteep)]
    public void TiltFor_MapsEachBand(RatesRegime regime, string expected)
        => RegimeClassifier.TiltFor(regime).Should().Be(expected);

    [Fact]
    public void Classification_IsDeterministic()
    {
        var a = RegimeClassifier.AssessRates(3.71m, 4.08m, Options);
        var b = RegimeClassifier.AssessRates(3.71m, 4.08m, Options);
        a.Should().Be(b);
    }
}
