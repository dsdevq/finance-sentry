namespace FinanceSentry.Modules.Research.API.Responses;

public record QuoteDto(
    string Ticker,
    decimal Price,
    decimal? PreviousClose,
    decimal? ChangePct,
    string Currency,
    DateTimeOffset FetchedAt);
