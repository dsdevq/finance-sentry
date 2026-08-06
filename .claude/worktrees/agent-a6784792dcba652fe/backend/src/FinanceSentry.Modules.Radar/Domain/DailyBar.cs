namespace FinanceSentry.Modules.Radar.Domain;

/// <summary>A single trading day's OHLCV + adjusted close for one ticker. Immutable once written.</summary>
public sealed class DailyBar
{
    public Guid Id { get; init; }
    public string Ticker { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }

    /// <summary>Adjusted close — used for all return math (splits/divs).</summary>
    public decimal AdjClose { get; set; }

    public long Volume { get; set; }
}
