using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.MarketStructure;

public sealed class BreadthTests
{
    [Fact]
    public void Compute_ReturnsAllNull_WhenNoTickers()
    {
        var result = Breadth.Compute([]);
        result.PctAboveMa20.Should().BeNull();
        result.Evaluated.Should().Be(0);
    }

    [Fact]
    public void Compute_CountsPercentAboveEachMa()
    {
        var states = new[]
        {
            new Breadth.TickerMaState(110m, Ma20: 100m, Ma50: 120m, Ma200: 100m), // above 20, below 50, above 200
            new Breadth.TickerMaState(90m, Ma20: 100m, Ma50: 80m, Ma200: 100m),   // below 20, above 50, below 200
        };

        var result = Breadth.Compute(states);

        result.PctAboveMa20.Should().Be(0.5m);
        result.PctAboveMa50.Should().Be(0.5m);
        result.PctAboveMa200.Should().Be(0.5m);
        result.Evaluated.Should().Be(2);
    }

    [Fact]
    public void Compute_IgnoresTickersWithNullMa_ForThatMa()
    {
        var states = new[]
        {
            new Breadth.TickerMaState(110m, Ma20: 100m, Ma50: null, Ma200: null),
            new Breadth.TickerMaState(90m, Ma20: null, Ma50: null, Ma200: null),
        };

        var result = Breadth.Compute(states);

        result.PctAboveMa20.Should().Be(1m);   // only the first is evaluable, and it's above
        result.PctAboveMa50.Should().BeNull(); // none evaluable
        result.Evaluated.Should().Be(1);
    }
}
