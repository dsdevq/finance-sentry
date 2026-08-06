using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Risk.Tests;

public sealed class AddToBrokenThesisTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly AddToBrokenThesisDetector _detector = new();

    private static HoldingSnapshot Snap(string symbol, decimal qty, DateTimeOffset at) => new()
    {
        UserId = UserId,
        Symbol = symbol,
        Sleeve = RiskSleeve.Brokerage,
        Quantity = qty,
        UsdValue = qty * 10,
        CapturedAt = at,
    };

    [Fact]
    public void QuantityIncrease_AfterBreak_IsFlagged()
    {
        var brokenAt = DateTimeOffset.UtcNow.AddDays(-10);
        var history = new List<HoldingSnapshot>
        {
            Snap("DRAM", 100m, brokenAt.AddDays(-5)),
            Snap("DRAM", 150m, brokenAt.AddDays(5)), // increase after break
        };
        var broken = new List<BrokenThesisSummary> { new(Guid.NewGuid(), "DRAM", brokenAt) };

        var flags = _detector.Detect(history, broken);

        flags.Should().ContainSingle(f => f.Ticker == "DRAM");
    }

    [Fact]
    public void QuantityIncrease_BeforeBreak_IsNotFlagged()
    {
        var brokenAt = DateTimeOffset.UtcNow.AddDays(-10);
        var history = new List<HoldingSnapshot>
        {
            Snap("DRAM", 100m, brokenAt.AddDays(-20)),
            Snap("DRAM", 150m, brokenAt.AddDays(-15)), // increase, but before the break
        };
        var broken = new List<BrokenThesisSummary> { new(Guid.NewGuid(), "DRAM", brokenAt) };

        var flags = _detector.Detect(history, broken);

        flags.Should().BeEmpty();
    }

    [Fact]
    public void NoBrokenThesisForSymbol_IsNotFlagged()
    {
        var history = new List<HoldingSnapshot>
        {
            Snap("AAPL", 10m, DateTimeOffset.UtcNow.AddDays(-5)),
            Snap("AAPL", 20m, DateTimeOffset.UtcNow),
        };

        var flags = _detector.Detect(history, []);

        flags.Should().BeEmpty();
    }

    [Fact]
    public void MultipleIncreasesAfterBreak_AreFlaggedOncePerIncrease()
    {
        var brokenAt = DateTimeOffset.UtcNow.AddDays(-30);
        var history = new List<HoldingSnapshot>
        {
            Snap("DRAM", 100m, brokenAt.AddDays(1)),
            Snap("DRAM", 120m, brokenAt.AddDays(10)),
            Snap("DRAM", 150m, brokenAt.AddDays(20)),
        };
        var broken = new List<BrokenThesisSummary> { new(Guid.NewGuid(), "DRAM", brokenAt) };

        var flags = _detector.Detect(history, broken);

        flags.Should().HaveCount(2);
    }
}
