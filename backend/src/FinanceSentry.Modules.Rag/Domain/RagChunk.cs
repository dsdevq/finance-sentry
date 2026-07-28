namespace FinanceSentry.Modules.Rag.Domain;

public sealed class RagChunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string ChunkText { get; private set; } = string.Empty;

    /// <summary>Zero-based position of this chunk within the document.</summary>
    public int Ordinal { get; private set; }

    /// <summary>Optional section label (e.g. "Management Discussion" for filings).</summary>
    public string? Section { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    // Embedding and content_tsv are Postgres-only columns managed via raw SQL in migrations.
    // They are not mapped as EF properties to keep the domain/InMemory-test path clean.
    // The EfCorpusRepository.SearchAsync uses FromSqlRaw to read them.

    private RagChunk() { }

    public static RagChunk Create(
        Guid documentId,
        string chunkText,
        int ordinal,
        string? section = null)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId must not be empty.", nameof(documentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(chunkText);
        if (ordinal < 0)
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Ordinal must be non-negative.");

        return new RagChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkText = chunkText,
            Ordinal = ordinal,
            Section = section,
            AddedAt = DateTimeOffset.UtcNow,
        };
    }
}
