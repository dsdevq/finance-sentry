namespace FinanceSentry.Modules.Retention.Application.Services;

using FinanceSentry.Modules.Retention.Application.Downsamplers;

/// <summary>
/// Builds the downsample SQL (feature 024, US3). Identifiers come only from the compiled
/// <see cref="DownsampleTarget"/> and are quoted exact-case; the cutoff is a bound <c>@cutoff</c>
/// parameter. The statement keeps the newest row per (partition, ISO-week) beyond the cutoff and
/// deletes the rest via <c>ctid</c>, so it works regardless of the table's primary key.
/// </summary>
public static class DownsampleSql
{
    /// <summary>Counts rows that a downsample of <paramref name="target"/> would remove.</summary>
    public static string CountRemovable(DownsampleTarget target) =>
        $"SELECT count(*)::bigint FROM ({Ranked(target)}) ranked WHERE rn > 1";

    /// <summary>Deletes all but the weekly-latest row per series, older than the cutoff.</summary>
    public static string KeepLatestPerWeek(DownsampleTarget target) =>
        $"DELETE FROM {target.QuotedTable} a USING ({Ranked(target)}) b " +
        "WHERE a.ctid = b.ctid AND b.rn > 1";

    private static string Ranked(DownsampleTarget target)
    {
        var partition = string.Join(", ", target.PartitionColumns.Select(c => $"\"{c}\""));
        return
            $"SELECT ctid, row_number() OVER (PARTITION BY {partition}, " +
            $"date_trunc('week', {target.QuotedTimestamp}) ORDER BY {target.QuotedTimestamp} DESC) AS rn " +
            $"FROM {target.QuotedTable} WHERE {target.QuotedTimestamp} < @cutoff";
    }
}
