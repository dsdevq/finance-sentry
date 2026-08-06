using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.MarketStructure;

public sealed class SectorRotationTests
{
    [Fact]
    public void Rank_OrdersByRelativeStrengthDescending()
    {
        var rs = new Dictionary<string, decimal?>
        {
            ["XLK"] = 0.10m,
            ["XLE"] = 0.02m,
            ["XLF"] = -0.05m,
        };

        var ranks = SectorRotation.Rank(rs);

        ranks["XLK"].Should().Be(1);
        ranks["XLE"].Should().Be(2);
        ranks["XLF"].Should().Be(3);
    }

    [Fact]
    public void Rank_PlacesNullRelativeStrengthLast()
    {
        var rs = new Dictionary<string, decimal?>
        {
            ["XLK"] = 0.10m,
            ["XLE"] = null,
        };

        SectorRotation.Rank(rs)["XLE"].Should().Be(2);
    }

    [Fact]
    public void BuildRows_ComputesRankDeltaVsPrior()
    {
        var current = new Dictionary<string, decimal?> { ["XLK"] = 0.01m, ["XLE"] = 0.05m };
        // Prior: XLK was #1, XLE #2. Now XLE leads.
        var priorRanks = new Dictionary<string, int> { ["XLK"] = 1, ["XLE"] = 2 };

        var rows = SectorRotation.BuildRows(63, current, priorRanks);

        var xle = rows.Single(r => r.Sector == "XLE");
        var xlk = rows.Single(r => r.Sector == "XLK");
        xle.Rank.Should().Be(1);
        xle.RankDelta.Should().Be(-1); // moved up one place
        xlk.RankDelta.Should().Be(1);  // fell one place
    }

    [Fact]
    public void BuildRows_HasNullDelta_WhenNoPriorRanks()
    {
        var current = new Dictionary<string, decimal?> { ["XLK"] = 0.01m };
        var rows = SectorRotation.BuildRows(63, current, null);
        rows.Single().RankDelta.Should().BeNull();
    }
}
