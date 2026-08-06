namespace FinanceSentry.Modules.Research.API.Responses;

public record NewsArticleDto(
    Guid Id,
    string Source,
    string Title,
    string Url,
    string? Summary,
    IReadOnlyList<string> Tickers,
    IReadOnlyList<string> Categories,
    DateTimeOffset PublishedAt);

public record NewsSourceHealthDto(
    Guid SourceId,
    string Name,
    string Url,
    int ConsecutiveFailures,
    DateTimeOffset? LastSuccessAt,
    string? LastFailureReason,
    string Status);

public record SearchMarketNewsResult(
    IReadOnlyList<NewsArticleDto> Articles,
    IReadOnlyList<NewsSourceHealthDto> SourceHealth,
    string Coverage,
    DateTimeOffset RetrievedAt);
