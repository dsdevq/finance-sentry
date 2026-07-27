namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class ResearchDocumentRepository(ResearchDbContext db) : IResearchDocumentRepository
{
    public async Task<IReadOnlyList<ResearchDocumentIdentity>> ListIdentitiesAsync(CancellationToken ct = default)
        => await db.ResearchDocuments.AsNoTracking()
            .Select(d => new ResearchDocumentIdentity(
                d.Id, d.SourceType, d.SourceId, d.UserId, d.ContentHash, d.IndexStatus))
            .ToListAsync(ct);

    public Task<ResearchDocument?> GetAsync(Guid id, CancellationToken ct = default)
        => db.ResearchDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(ResearchDocument document, CancellationToken ct = default)
    {
        db.ResearchDocuments.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ResearchDocument document, CancellationToken ct = default)
    {
        db.ResearchDocuments.Update(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ResearchDocument>> ListByStatusAsync(
        ResearchIndexStatus status, int limit, CancellationToken ct = default)
        => await db.ResearchDocuments
            .Where(d => d.IndexStatus == status)
            .OrderBy(d => d.CapturedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ResearchChunk>> ListChunksAsync(Guid documentId, CancellationToken ct = default)
        => await db.ResearchChunks.AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.Ordinal)
            .ToListAsync(ct);

    public async Task AddChunksAsync(IReadOnlyList<ResearchChunk> chunks, CancellationToken ct = default)
    {
        db.ResearchChunks.AddRange(chunks);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveChunksAsync(IReadOnlyList<Guid> chunkIds, CancellationToken ct = default)
    {
        // Explicit two-step delete instead of ExecuteDeleteAsync/cascade so the same path works
        // on the EF InMemory provider used by unit and parity tests.
        var embeddings = await db.ResearchEmbeddings.Where(e => chunkIds.Contains(e.ChunkId)).ToListAsync(ct);
        db.ResearchEmbeddings.RemoveRange(embeddings);
        var chunks = await db.ResearchChunks.Where(c => chunkIds.Contains(c.Id)).ToListAsync(ct);
        db.ResearchChunks.RemoveRange(chunks);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ResearchEmbedding>> ListEmbeddingsForChunksAsync(
        IReadOnlyList<Guid> chunkIds, CancellationToken ct = default)
        => await db.ResearchEmbeddings.AsNoTracking()
            .Where(e => chunkIds.Contains(e.ChunkId))
            .ToListAsync(ct);

    public async Task AddEmbeddingsAsync(IReadOnlyList<ResearchEmbedding> embeddings, CancellationToken ct = default)
    {
        db.ResearchEmbeddings.AddRange(embeddings);
        await db.SaveChangesAsync(ct);
    }
}
