namespace FinanceSentry.Modules.Research.API.Responses;

public record QuoteDto(
    string Ticker,
    string? ResolvedTicker,
    decimal Price,
    decimal? PreviousClose,
    decimal? ChangePct,
    string Currency,
    DateTimeOffset FetchedAt,
    string MarketState,
    string Session,
    bool IsStale,
    DateTimeOffset? SourcePriceTime,
    DateTimeOffset? RegularMarketTime);
