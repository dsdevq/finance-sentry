using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.MarketStructure;

public sealed class VolatilityTests
{
    [Fact]
    public void StdDev_IsNull_WhenFewerThanWindowReturns()
    {
        var closes = Enumerable.Range(0, 10).Select(i => 100m + i).ToArray();
        Volatility.StdDev(closes, 63).Should().BeNull("short history is not evaluable");
    }

    [Fact]
    public void StdDev_IsPositive_ForVaryingSeries()
    {
        // Alternating up/down closes → non-zero volatility.
        var closes = new List<decimal>();
        for (var i = 0; i < 70; i++)
        {
            closes.Add(i % 2 == 0 ? 100m : 104m);
        }

        Volatility.StdDev(closes, 63).Should().NotBeNull();
        Volatility.StdDev(closes, 63)!.Value.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void TodayZScore_FlagsThreeSigmaMove()
    {
        // 64 flat closes (σ≈0 avoided by tiny wiggle), then a big drop on the last day.
        var closes = new List<decimal>();
        for (var i = 0; i < 64; i++)
        {
            closes.Add(i % 2 == 0 ? 100m : 100.5m); // small daily noise
        }

        closes.Add(80m); // ~-20% crash day

        var z = Volatility.TodayZScore(closes, 63);
        z.Should().NotBeNull();
        Math.Abs(z!.Value).Should().BeGreaterThan(3m);
    }

    [Fact]
    public void VolumeRatio_IsNull_WhenTrailingAverageIsZero()
    {
        var volumes = new long[] { 0, 0, 0, 0, 0, 500 };
        Volatility.VolumeRatio(volumes, 5).Should().BeNull("zero-volume history is not evaluable");
    }

    [Fact]
    public void VolumeRatio_ComputesTodayOverTrailingAverage()
    {
        var volumes = new long[] { 100, 100, 100, 100, 100, 300 };
        Volatility.VolumeRatio(volumes, 5).Should().Be(3m);
    }

    [Fact]
    public void VolumeRatio_IsNull_WhenTooFewBars()
    {
        var volumes = new long[] { 100, 200 };
        Volatility.VolumeRatio(volumes, 20).Should().BeNull();
    }
}
