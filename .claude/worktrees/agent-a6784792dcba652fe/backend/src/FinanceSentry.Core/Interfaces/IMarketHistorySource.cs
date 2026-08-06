namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Swappable daily price-history source (Yahoo chart in v1; a Stooq fallback can drop in
/// without touching ingestion/consumers). Returns full OHLCV + adjusted close.
/// </summary>
public interface IMarketHistorySource
{
    Task<IReadOnlyList<DailyBarData>> GetDailyBarsAsync(
        string ticker, DateOnly since, CancellationToken ct = default);
}

public sealed record DailyBarData(
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal AdjClose,
    long Volume);
