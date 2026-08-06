namespace FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>Slim projection used to diff stored documents against current source content without loading text.</summary>
public record ResearchDocumentIdentity(
    Guid Id,
    ResearchDocumentSourceType SourceType,
    string SourceId,
    Guid? UserId,
    string ContentHash,
    ResearchIndexStatus IndexStatus);

public interface IResearchDocumentRepository
{
    Task<IReadOnlyList<ResearchDocumentIdentity>> ListIdentitiesAsync(CancellationToken ct = default);

    Task<ResearchDocument?> GetAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(ResearchDocument document, CancellationToken ct = default);

    Task UpdateAsync(ResearchDocument document, CancellationToken ct = default);

    Task<IReadOnlyList<ResearchDocument>> ListByStatusAsync(
        ResearchIndexStatus status, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<ResearchChunk>> ListChunksAsync(Guid documentId, CancellationToken ct = default);

    Task AddChunksAsync(IReadOnlyList<ResearchChunk> chunks, CancellationToken ct = default);

    Task RemoveChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken ct = default);

    Task<IReadOnlyList<ResearchEmbedding>> ListEmbeddingsForChunksAsync(
        IReadOnlyList<Guid> chunkIds, CancellationToken ct = default);

    Task AddEmbeddingsAsync(IReadOnlyList<ResearchEmbedding> embeddings, CancellationToken ct = default);
}
