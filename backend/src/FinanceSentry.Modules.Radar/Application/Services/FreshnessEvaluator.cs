namespace FinanceSentry.Modules.Radar.Application.Services;

/// <summary>Deterministic staleness check: a latest-bar date older than N trading days is stale.</summary>
public static class FreshnessEvaluator
{
    // Weekend/holiday cushion when converting a trading-day bound to a calendar-day gap.
    private const int CalendarCushionDays = 5;

    public static bool IsStale(DateOnly? latestDate, DateOnly today, int maxTradingDays)
    {
        if (latestDate is null)
        {
            return true;
        }

        var calendarGap = today.DayNumber - latestDate.Value.DayNumber;
        return calendarGap > maxTradingDays + CalendarCushionDays;
    }
}
