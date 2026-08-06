namespace FinanceSentry.Modules.Retention.Infrastructure.Backup;

/// <summary>
/// Deterministic object-key naming for backup artifacts (feature 024, US2). Kept pure so the
/// daily/weekly routing and filename shape are unit-testable without running a backup.
/// </summary>
public static class BackupNaming
{
    public const string DailyPrefix = "daily/";
    public const string WeeklyPrefix = "weekly/";

    /// <summary>True when <paramref name="now"/> falls on the configured weekly ISO day (1=Mon..7=Sun).</summary>
    public static bool IsWeekly(DateTimeOffset now, int weeklyOn) => IsoDay(now) == weeklyOn;

    /// <summary>The prefix a backup taken at <paramref name="now"/> is stored under.</summary>
    public static string PrefixFor(DateTimeOffset now, int weeklyOn) =>
        IsWeekly(now, weeklyOn) ? WeeklyPrefix : DailyPrefix;

    /// <summary>Encrypted-dump filename, e.g. <c>2026-08-06T02-00-00Z.dump.age</c>.</summary>
    public static string FileName(DateTimeOffset now) => $"{now:yyyy-MM-ddTHH-mm-ssZ}.dump.age";

    /// <summary>Full object key: prefix + filename.</summary>
    public static string KeyFor(DateTimeOffset now, int weeklyOn) => PrefixFor(now, weeklyOn) + FileName(now);

    private static int IsoDay(DateTimeOffset now) => (int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek;
}
