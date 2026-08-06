namespace FinanceSentry.Modules.Research.Domain;

public class QuoteCacheEntry
{
    public string Ticker { get; set; } = string.Empty;
    public string? ResolvedTicker { get; set; }
    public decimal Price { get; set; }
    public decimal? PreviousClose { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
    public string MarketState { get; set; } = "unknown";
    public string Session { get; set; } = "unknown";
    public bool IsStale { get; set; }
    public DateTimeOffset? SourcePriceTime { get; set; }
    public DateTimeOffset? RegularMarketTime { get; set; }
}
