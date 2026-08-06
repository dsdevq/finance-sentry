namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public record ResearchRetrievalRequest(
    Guid UserId,
    string Query,
    IReadOnlyList<string> Tickers,
    Guid? ThesisId,
    IReadOnlyList<ResearchDocumentSourceType> SourceTypes,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Limit);

public record ResearchRetrievalHit(
    ResearchDocument Document,
    ResearchChunk Chunk,
    double SemanticScore,
    double LexicalScore,
    double CombinedScore);

public record ResearchRetrievalResult(IReadOnlyList<ResearchRetrievalHit> Hits, int TotalMatched);

public interface IResearchRetriever
{
    /// <summary>
    /// Hybrid search over indexed chunks: structured filters narrow candidates, then semantic
    /// similarity (when embeddings exist) and lexical overlap are combined into one ranking.
    /// </summary>
    Task<ResearchRetrievalResult> SearchAsync(ResearchRetrievalRequest request, CancellationToken ct = default);
}
