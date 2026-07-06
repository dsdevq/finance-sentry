namespace FinanceSentry.Modules.Research.API.Responses;

public record EarningsEventDto(
    string Ticker,
    string EventType,
    DateOnly EventDate,
    bool IsEstimate,
    string Source);
