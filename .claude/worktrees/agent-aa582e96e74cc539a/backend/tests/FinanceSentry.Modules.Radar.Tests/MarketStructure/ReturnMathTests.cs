using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.MarketStructure;

public sealed class ReturnMathTests
{
    private static decimal[] Ramp(int count, decimal start, decimal step)
    {
        var arr = new decimal[count];
        for (var i = 0; i < count; i++)
        {
            arr[i] = start + i * step;
        }

        return arr;
    }

    [Fact]
    public void Return_ComputesSimpleReturnOverWindow()
    {
        // 22 bars: last=121, close 21 days ago=100 → (121-100)/100 = 0.21
        var closes = Ramp(22, 100m, 1m);
        ReturnMath.Return(closes, 21).Should().Be(0.21m);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(63)]
    [InlineData(126)]
    [InlineData(252)]
    public void Return_IsNull_WhenFewerThanWindowPlusOneBars(int window)
    {
        var closes = Ramp(window, 100m, 1m); // exactly window bars, need window+1
        ReturnMath.Return(closes, window).Should().BeNull("short history is not evaluable, never zero");
    }

    [Fact]
    public void Return_IsNull_WhenPastCloseIsZero()
    {
        var closes = new decimal[] { 0m, 10m };
        ReturnMath.Return(closes, 1).Should().BeNull();
    }

    [Fact]
    public void RelativeStrength_IsTickerMinusBenchmark()
    {
        ReturnMath.RelativeStrength(0.10m, 0.04m).Should().Be(0.06m);
    }

    [Fact]
    public void RelativeStrength_IsNull_WhenEitherSideNull()
    {
        ReturnMath.RelativeStrength(null, 0.04m).Should().BeNull();
        ReturnMath.RelativeStrength(0.10m, null).Should().BeNull();
    }

    [Fact]
    public void RelativeStrength_OrdersOutperformerAboveBenchmarkAboveUnderperformer()
    {
        // A outperforms SPY, B underperforms over 21 days (Independent Test for US2).
        var a = Ramp(22, 100m, 2m);   // +42/100 = 0.42
        var spy = Ramp(22, 100m, 1m); // +21/100 = 0.21
        var b = Ramp(22, 100m, 0.5m); // +10.5/100 = 0.105

        var spyReturn = ReturnMath.Return(spy, 21);
        var rsA = ReturnMath.RelativeStrength(ReturnMath.Return(a, 21), spyReturn);
        var rsB = ReturnMath.RelativeStrength(ReturnMath.Return(b, 21), spyReturn);

        rsA.Should().BeGreaterThan(0m);
        rsB.Should().BeLessThan(0m);
        rsA.Should().BeGreaterThan(rsB!.Value);
    }
}
