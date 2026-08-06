namespace FinanceSentry.Modules.Research.Application.Services;

public record ResearchIndexingResult(int Synced, int Processed, int Indexed, int Skipped, int Failed);

public interface IResearchIndexer
{
    /// <summary>
    /// Syncs source entities into research documents, then chunks and embeds every pending or
    /// previously failed document. Idempotent: unchanged content creates no duplicate chunks or
    /// embeddings; per-document failures are recorded without blocking other documents.
    /// </summary>
    Task<ResearchIndexingResult> IndexPendingAsync(CancellationToken ct = default);
}
