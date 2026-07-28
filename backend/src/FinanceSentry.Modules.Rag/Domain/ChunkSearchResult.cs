namespace FinanceSentry.Modules.Rag.Domain;

/// <summary>
/// One retrieved passage with full citation provenance.
/// Numbers inside <see cref="ChunkText"/> are context to cite — never treated as authoritative figures.
/// </summary>
public sealed record ChunkSearchResult(
    Guid ChunkId,
    Guid DocumentId,
    DocType DocType,
    string Title,
    string? Url,
    string? Ticker,
    DateTimeOffset PublishedAt,
    string ChunkText,
    int Ordinal,
    string? Section,
    double Score);
