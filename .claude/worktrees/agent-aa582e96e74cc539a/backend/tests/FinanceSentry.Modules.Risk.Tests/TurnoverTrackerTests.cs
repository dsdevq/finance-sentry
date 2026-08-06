using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Risk.Tests;

public sealed class TurnoverTrackerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly TurnoverTracker _tracker = new();

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
    public void CountsQuantityIncreaseEvents_WithinRollingQuarter()
    {
        var now = DateTimeOffset.UtcNow;
        var history = new List<HoldingSnapshot>
        {
            Snap("NVDA", 10m, now.AddDays(-60)),
            Snap("NVDA", 15m, now.AddDays(-30)), // increase → counted
            Snap("AAPL", 5m, now.AddDays(-20)),
            Snap("AAPL", 8m, now.AddDays(-10)), // increase → counted
        };

        _tracker.CountDiscretionaryTradesInRollingQuarter(history, now).Should().Be(2);
    }

    [Fact]
    public void Decrease_IsNeverCountedAsATrade()
    {
        var now = DateTimeOffset.UtcNow;
        var history = new List<HoldingSnapshot>
        {
            Snap("NVDA", 20m, now.AddDays(-30)),
            Snap("NVDA", 10m, now.AddDays(-10)), // decrease → not counted
        };

        _tracker.CountDiscretionaryTradesInRollingQuarter(history, now).Should().Be(0);
    }

    [Fact]
    public void QuarterRollover_ExcludesEventsOlderThan90Days()
    {
        var now = DateTimeOffset.UtcNow;
        var history = new List<HoldingSnapshot>
        {
            Snap("NVDA", 10m, now.AddDays(-200)),
            Snap("NVDA", 15m, now.AddDays(-120)), // increase, but before the rolling window
        };

        _tracker.CountDiscretionaryTradesInRollingQuarter(history, now).Should().Be(0);
    }
}
