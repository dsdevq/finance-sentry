namespace FinanceSentry.Modules.Retention.Tests;

using FinanceSentry.Modules.Retention.Application.Downsamplers;
using FinanceSentry.Modules.Retention.Application.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// The downsample statement shape (feature 024, US3): keep the newest row per (partition, ISO-week)
/// beyond the cutoff, delete the rest via ctid, with a bound cutoff parameter and exact-case identifiers.
/// </summary>
public sealed class DownsampleSqlTests
{
    private static readonly DownsampleTarget Bars =
        new("radar", "daily_bars", "Date", ["Ticker"], 365);
    private static readonly DownsampleTarget NetWorth =
        new("public", "net_worth_snapshots", "SnapshotDate", ["UserId"], 365);

    [Fact]
    public void Keep_latest_per_week_partitions_by_series_and_iso_week()
    {
        var sql = DownsampleSql.KeepLatestPerWeek(Bars);

        sql.Should().Contain("DELETE FROM \"radar\".\"daily_bars\" a USING");
        sql.Should().Contain("PARTITION BY \"Ticker\", date_trunc('week', \"Date\")");
        sql.Should().Contain("ORDER BY \"Date\" DESC");
        sql.Should().Contain("a.ctid = b.ctid AND b.rn > 1");
        sql.Should().Contain("< @cutoff");
    }

    [Fact]
    public void Count_removable_counts_the_non_latest_rows()
    {
        DownsampleSql.CountRemovable(NetWorth)
            .Should().Contain("WHERE rn > 1").And.Contain("\"public\".\"net_worth_snapshots\"");
    }

    [Fact]
    public void Net_worth_target_partitions_by_user()
    {
        DownsampleSql.KeepLatestPerWeek(NetWorth)
            .Should().Contain("PARTITION BY \"UserId\", date_trunc('week', \"SnapshotDate\")")
            .And.Contain("@cutoff");
    }

    [Fact]
    public void Targets_cover_the_two_downsampled_tables()
    {
        DownsampleTargets.All.Select(t => t.QualifiedName)
            .Should().BeEquivalentTo(["radar.daily_bars", "public.net_worth_snapshots"]);
    }
}
