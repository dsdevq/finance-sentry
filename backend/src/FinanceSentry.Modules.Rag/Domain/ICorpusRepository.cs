namespace FinanceSentry.Modules.Rag.Domain;

public interface ICorpusRepository
{
    Task AddDocumentAsync(RagDocument document, CancellationToken ct = default);
    Task AddChunksAsync(IReadOnlyList<RagChunk> chunks, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Hybrid retrieval: cosine-similarity over the pgvector HNSW index (dense) fused with
    /// Postgres full-text search (keyword) via Reciprocal Rank Fusion (k=60).
    /// Returns at most <see cref="CorpusSearchQuery.TopK"/> results ordered by fused score.
    /// </summary>
    Task<IReadOnlyList<ChunkSearchResult>> SearchAsync(
        CorpusSearchQuery query, CancellationToken ct = default);
}
