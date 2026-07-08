namespace FinanceSentry.Modules.Radar.Application.Services;

/// <summary>Deterministic staleness check: a latest-bar date older than N trading days is stale.</summary>
public static class FreshnessEvaluator
{
    public static bool IsStale(DateOnly? latestDate, DateOnly today, int maxTradingDays)
    {
        if (latestDate is null)
        {
            return true;
        }

        return TradingDaysBetween(latestDate.Value, today) > maxTradingDays;
    }

    /// <summary>
    /// Weekday count in (<paramref name="from"/>, <paramref name="to"/>] — an exchange-holiday-
    /// blind approximation that errs at most one day lenient around holidays, instead of the flat
    /// +5 calendar cushion that let a week of dead data pass as fresh.
    /// </summary>
    public static int TradingDaysBetween(DateOnly from, DateOnly to)
    {
        if (to <= from)
        {
            return 0;
        }

        var count = 0;
        for (var d = from.AddDays(1); d <= to; d = d.AddDays(1))
        {
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                count++;
            }
        }

        return count;
    }
}
