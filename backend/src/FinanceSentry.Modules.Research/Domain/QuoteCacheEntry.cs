namespace FinanceSentry.Modules.Research.Domain;

public class QuoteCacheEntry
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? PreviousClose { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
