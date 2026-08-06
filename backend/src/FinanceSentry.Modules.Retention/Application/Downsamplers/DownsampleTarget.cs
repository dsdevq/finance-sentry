namespace FinanceSentry.Modules.Retention.Application.Downsamplers;

/// <summary>
/// A table to downsample to weekly resolution beyond its window (feature 024, US3). Exact-case
/// identifiers, as with <c>RetentionPolicy</c>. Downsampling keeps the newest row per
/// (partition, ISO-week) and deletes the rest — coarser resolution via deletion only, so there are no
/// synthetic rows to get wrong and chart continuity is preserved (one point per week).
/// </summary>
/// <param name="Schema">Schema, exact case.</param>
/// <param name="Table">Table, exact case.</param>
/// <param name="TimestampColumn">Ordering/bucketing column, exact case.</param>
/// <param name="PartitionColumns">Columns that scope a series (e.g. Ticker, UserId), exact case.</param>
/// <param name="WindowDays">Rows older than this are downsampled.</param>
public sealed record DownsampleTarget(
    string Schema,
    string Table,
    string TimestampColumn,
    IReadOnlyList<string> PartitionColumns,
    int WindowDays)
{
    public string QualifiedName => $"{Schema}.{Table}";

    public string QuotedTable => $"\"{Schema}\".\"{Table}\"";

    public string QuotedTimestamp => $"\"{TimestampColumn}\"";
}

/// <summary>The tables downsampled by the gated <c>DownsampleJob</c> (research D4/D9).</summary>
public static class DownsampleTargets
{
    private const int OneYear = 365;

    public static readonly IReadOnlyList<DownsampleTarget> All =
    [
        // Daily OHLC → one bar per ISO week (the week's latest) beyond a year.
        new("radar", "daily_bars", "Date", ["Ticker"], OneYear),
        // One net-worth snapshot per ISO week per user beyond a year.
        new("public", "net_worth_snapshots", "SnapshotDate", ["UserId"], OneYear),
    ];
}
