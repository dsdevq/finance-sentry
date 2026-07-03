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
