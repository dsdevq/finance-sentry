namespace FinanceSentry.Modules.Rag.Domain;

/// <summary>
/// Parameters for a hybrid (dense + keyword) corpus search.
/// <see cref="QueryEmbedding"/> is the pre-computed embedding of <see cref="QueryText"/>.
/// </summary>
public sealed record CorpusSearchQuery(
    string QueryText,
    float[] QueryEmbedding,
    string? Ticker = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int TopK = 10);
