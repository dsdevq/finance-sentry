namespace FinanceSentry.Modules.Retention.Tests;

using FinanceSentry.Modules.Retention.Infrastructure.Backup;
using FluentAssertions;
using Xunit;

/// <summary>Daily/weekly routing and filename shape for backup artifacts (feature 024, US2).</summary>
public sealed class BackupNamingTests
{
    // 2026-08-09 is a Sunday (ISO day 7); 2026-08-10 is a Monday (ISO day 1).
    private static readonly DateTimeOffset Sunday = new(2026, 8, 9, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Monday = new(2026, 8, 10, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Weekly_day_routes_to_weekly_prefix()
    {
        BackupNaming.IsWeekly(Sunday, weeklyOn: 7).Should().BeTrue();
        BackupNaming.KeyFor(Sunday, weeklyOn: 7).Should().StartWith("weekly/");
    }

    [Fact]
    public void Non_weekly_day_routes_to_daily_prefix()
    {
        BackupNaming.IsWeekly(Monday, weeklyOn: 7).Should().BeFalse();
        BackupNaming.KeyFor(Monday, weeklyOn: 7).Should().StartWith("daily/");
    }

    [Fact]
    public void Filename_is_sortable_utc_with_age_extension()
    {
        BackupNaming.FileName(Sunday).Should().Be("2026-08-09T02-00-00Z.dump.age");
    }

    [Fact]
    public void Key_is_prefix_plus_filename()
    {
        BackupNaming.KeyFor(Monday, weeklyOn: 7)
            .Should().Be("daily/2026-08-10T02-00-00Z.dump.age");
    }
}
