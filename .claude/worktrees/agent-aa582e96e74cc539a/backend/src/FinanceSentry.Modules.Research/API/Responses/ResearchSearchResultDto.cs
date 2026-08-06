namespace FinanceSentry.Modules.Research.API.Responses;

public record ResearchSearchResultDto(
    string Query,
    IReadOnlyList<ResearchSearchHitDto> Results,
    DateTimeOffset RetrievedAt);

public record ResearchSearchHitDto(
    Guid DocumentId,
    Guid ChunkId,
    string SourceType,
    string? SourceName,
    string Title,
    string? CanonicalUrl,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CapturedAt,
    IReadOnlyList<string> Tickers,
    IReadOnlyList<Guid> ThesisIds,
    string Snippet,
    double SemanticScore,
    double LexicalScore,
    double CombinedScore);
