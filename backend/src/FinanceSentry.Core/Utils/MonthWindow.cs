namespace FinanceSentry.Core.Utils;

/// <summary>
/// Calendar-month window boundaries for the month-bucketed statistics.
/// </summary>
/// <remarks>
/// Every "last N months" window in the app must start on a month boundary. Deriving the
/// start with <c>DateTime.UtcNow.AddMonths(-n)</c> lands mid-month, so the oldest bucket
/// holds only a few days of transactions and reads as a collapsed bar or, worse, yields a
/// savings rate computed from a single day. Flooring to the first of the month makes
/// "3M" mean three whole calendar months plus the one in progress.
/// </remarks>
public static class MonthWindow
{
    /// <summary>
    /// UTC midnight on the first day of the month <paramref name="months"/> before the
    /// current one. With <paramref name="months"/> = 3 during August, this is 1 May — so the
    /// window spans three complete months (May, June, July) and the in-progress August.
    /// </summary>
    public static DateTime StartOfMonthsAgo(int months, DateTime? now = null)
    {
        var reference = now ?? DateTime.UtcNow;
        return new DateTime(reference.Year, reference.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-months);
    }
}
