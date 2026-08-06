namespace FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>
/// Structured candidate filter. Visibility is enforced here: candidates are limited to global
/// documents (null UserId) plus the caller's own private documents.
/// </summary>
public record ResearchRetrievalFilter(
    Guid UserId,
    IReadOnlyList<string> Tickers,
    Guid? ThesisId,
    IReadOnlyList<ResearchDocumentSourceType> SourceTypes,
    DateTimeOffset? From,
    DateTimeOffset? To);

/// <summary>A retrievable chunk with its parent document and the active embedding, when one exists.</summary>
public record ResearchChunkCandidate(
    ResearchDocument Document,
    ResearchChunk Chunk,
    ResearchEmbedding? Embedding);

public interface IResearchRetrievalRepository
{
    Task<IReadOnlyList<ResearchChunkCandidate>> ListCandidatesAsync(
        ResearchRetrievalFilter filter, CancellationToken ct = default);
}
