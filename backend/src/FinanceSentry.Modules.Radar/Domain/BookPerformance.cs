namespace FinanceSentry.Modules.Radar.Domain;

/// <summary>Lookback window for TWR computation.</summary>
public enum BookPerformancePeriod
{
    OneWeek,
    OneMonth,
    ThreeMonths,
    OneYear,
}

/// <summary>
/// Single-period TWR result for both the book (IBKR brokerage portfolio) and the SPY benchmark.
/// Null fields indicate insufficient price history for the period.
/// </summary>
public sealed record PeriodTwr(
    BookPerformancePeriod Period,
    DateOnly Since,
    decimal? BookTwr,
    decimal? SpyTwr,
    decimal? Delta,
    string? Verdict);

/// <summary>
/// TWR scoreboard across the four standard lookback windows.
/// </summary>
public sealed record BookPerformanceResult(
    IReadOnlyList<PeriodTwr> Periods,
    DateOnly AsOf)
{
    public static BookPerformanceResult Empty(DateOnly asOf) =>
        new([], asOf);
}
