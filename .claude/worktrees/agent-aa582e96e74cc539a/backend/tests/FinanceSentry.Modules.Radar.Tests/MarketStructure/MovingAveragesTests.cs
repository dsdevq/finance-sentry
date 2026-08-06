using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.MarketStructure;

public sealed class MovingAveragesTests
{
    [Fact]
    public void Sma_AveragesLastPeriodValues()
    {
        var values = new decimal[] { 1m, 2m, 3m, 4m, 5m };
        MovingAverages.Sma(values, 3).Should().Be(4m); // (3+4+5)/3
    }

    [Fact]
    public void Sma_IsNull_WhenFewerThanPeriod()
    {
        MovingAverages.Sma([1m, 2m], 50).Should().BeNull();
    }

    [Fact]
    public void Extension_ComputesDistanceFromMa()
    {
        MovingAverages.Extension(115m, 100m).Should().Be(0.15m);
    }

    [Fact]
    public void Extension_IsNull_WhenMaNullOrZero()
    {
        MovingAverages.Extension(115m, null).Should().BeNull();
        MovingAverages.Extension(115m, 0m).Should().BeNull();
    }
}
