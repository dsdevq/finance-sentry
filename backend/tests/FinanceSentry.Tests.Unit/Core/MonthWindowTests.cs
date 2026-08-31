namespace FinanceSentry.Tests.Unit.Core;

using FinanceSentry.Core.Utils;
using FluentAssertions;
using Xunit;

public class MonthWindowTests
{
    [Fact]
    public void StartOfMonthsAgo_LandsOnTheFirstOfTheMonth()
    {
        // Mid-month "now" is the case the old UtcNow.AddMonths(-n) got wrong: it started
        // the window on the 20th, so the oldest bucket held ten days and charted as a
        // near-zero bar next to full months.
        var now = new DateTime(2026, 8, 20, 13, 45, 0, DateTimeKind.Utc);

        MonthWindow.StartOfMonthsAgo(3, now).Should().Be(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void StartOfMonthsAgo_CrossesTheYearBoundary()
    {
        var now = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);

        MonthWindow.StartOfMonthsAgo(6, now).Should().Be(new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void StartOfMonthsAgo_IsUtcSoBucketKeysDoNotShiftWithTheHostTimeZone()
    {
        MonthWindow.StartOfMonthsAgo(1).Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void StartOfMonthsAgo_LeavesExactlyNCompleteMonthsBeforeTheCurrentOne()
    {
        // "3M" must mean three whole months plus the one in progress — the guarantee the
        // savings-rate chart relies on when it drops the in-progress bucket.
        var now = new DateTime(2026, 8, 31, 23, 59, 0, DateTimeKind.Utc);
        var since = MonthWindow.StartOfMonthsAgo(3, now);

        var completeMonths = ((now.Year - since.Year) * 12) + now.Month - since.Month;
        completeMonths.Should().Be(3);
    }
}
