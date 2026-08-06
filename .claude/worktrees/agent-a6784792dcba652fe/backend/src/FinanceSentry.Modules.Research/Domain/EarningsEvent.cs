namespace FinanceSentry.Modules.Research.Domain;

// In-memory only — earnings/ex-dividend dates are fetched live from Yahoo per request
// and never persisted (see YahooEarningsCalendarService).
public sealed record EarningsEvent(
    string Ticker,
    string EventType,
    DateOnly EventDate,
    bool IsEstimate,
    string Source);

public static class EarningsEventType
{
    public const string Earnings = "earnings";
    public const string ExDividend = "ex_dividend";
    public const string Dividend = "dividend";
}
