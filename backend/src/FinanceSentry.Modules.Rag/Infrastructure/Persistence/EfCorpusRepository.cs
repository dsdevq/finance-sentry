namespace FinanceSentry.Modules.Rag.Infrastructure.Persistence;

using System.Text;
using FinanceSentry.Modules.Rag.Domain;
using Microsoft.EntityFrameworkCore;

public sealed class EfCorpusRepository(RagDbContext db) : ICorpusRepository
{
    public async Task AddDocumentAsync(RagDocument document, CancellationToken ct = default)
        => await db.Documents.AddAsync(document, ct);

    public async Task AddChunksAsync(IReadOnlyList<RagChunk> chunks, CancellationToken ct = default)
        => await db.Chunks.AddRangeAsync(chunks, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    /// <summary>
    /// Hybrid retrieval: HNSW cosine-similarity (dense) + tsvector full-text (keyword),
    /// fused via Reciprocal Rank Fusion (k=60). Metadata filters applied before ranking.
    /// Soft recency decay (1/(1 + age_years)) applied to the fused score.
    /// </summary>
    public async Task<IReadOnlyList<ChunkSearchResult>> SearchAsync(
        CorpusSearchQuery query, CancellationToken ct = default)
    {
        // Serialize float[] to pgvector literal: '[1.0,2.0,...]'
        var vectorLiteral = FloatArrayToVectorLiteral(query.QueryEmbedding);

        // Parameters are passed positionally in Npgsql raw SQL.
        // Using CAST placeholders to avoid Npgsql type-inference issues.
        var sql = BuildSearchSql();

        var rows = await db.Database
            .SqlQueryRaw<ChunkSearchRow>(
                sql,
                vectorLiteral,
                query.QueryText,
                query.Ticker as object ?? DBNull.Value,
                query.FromDate as object ?? DBNull.Value,
                query.ToDate as object ?? DBNull.Value,
                query.TopK)
            .ToListAsync(ct);

        return rows.Select(r => new ChunkSearchResult(
            r.ChunkId,
            r.DocumentId,
            Enum.Parse<DocType>(r.DocType, ignoreCase: true),
            r.Title,
            r.Url,
            r.Ticker,
            r.PublishedAt,
            r.ChunkText,
            r.Ordinal,
            r.Section,
            r.Score))
            .ToList();
    }

    private static string BuildSearchSql() => """
        WITH params AS (
            SELECT
                {0}::vector                    AS q_vec,
                plainto_tsquery('english', {1}) AS q_tsv
        ),
        dense AS (
            SELECT c.id,
                   ROW_NUMBER() OVER (ORDER BY c.embedding <=> p.q_vec) AS rank
            FROM   rag.chunks c
            JOIN   rag.documents d ON d.id = c.document_id,
                   params p
            WHERE  c.embedding IS NOT NULL
              AND  ({2}::text IS NULL OR d.ticker = {2}::text)
              AND  ({3}::timestamptz IS NULL OR d.published_at >= {3}::timestamptz)
              AND  ({4}::timestamptz IS NULL OR d.published_at <= {4}::timestamptz)
            ORDER  BY c.embedding <=> p.q_vec
            LIMIT  30
        ),
        keyword AS (
            SELECT c.id,
                   ROW_NUMBER() OVER (ORDER BY ts_rank(c.content_tsv, p.q_tsv) DESC) AS rank
            FROM   rag.chunks c
            JOIN   rag.documents d ON d.id = c.document_id,
                   params p
            WHERE  c.content_tsv IS NOT NULL
              AND  c.content_tsv @@ p.q_tsv
              AND  ({2}::text IS NULL OR d.ticker = {2}::text)
              AND  ({3}::timestamptz IS NULL OR d.published_at >= {3}::timestamptz)
              AND  ({4}::timestamptz IS NULL OR d.published_at <= {4}::timestamptz)
            ORDER  BY ts_rank(c.content_tsv, p.q_tsv) DESC
            LIMIT  30
        ),
        rrf AS (
            SELECT COALESCE(dn.id, kw.id) AS id,
                   (COALESCE(1.0 / (60.0 + dn.rank), 0) +
                    COALESCE(1.0 / (60.0 + kw.rank), 0)) AS rrf_score
            FROM   dense dn
            FULL JOIN keyword kw USING (id)
        )
        SELECT
            c.id                                                              AS "ChunkId",
            d.id                                                              AS "DocumentId",
            d.doc_type                                                        AS "DocType",
            d.title                                                           AS "Title",
            d.url                                                             AS "Url",
            d.ticker                                                          AS "Ticker",
            d.published_at                                                    AS "PublishedAt",
            c.chunk_text                                                      AS "ChunkText",
            c.ordinal                                                         AS "Ordinal",
            c.section                                                         AS "Section",
            rrf.rrf_score / (1.0 + EXTRACT(EPOCH FROM (NOW() - d.published_at)) / 86400.0 / 365.0)
                                                                              AS "Score"
        FROM   rrf
        JOIN   rag.chunks    c ON c.id = rrf.id
        JOIN   rag.documents d ON d.id = c.document_id
        ORDER  BY "Score" DESC
        LIMIT  {5}
        """;

    private static string FloatArrayToVectorLiteral(float[] v)
    {
        var sb = new StringBuilder("[");
        for (var i = 0; i < v.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(v[i].ToString("G", System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }

    // Keyless projection type for SqlQueryRaw — not an EF entity.
    private sealed class ChunkSearchRow
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string DocType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Ticker { get; set; }
        public DateTimeOffset PublishedAt { get; set; }
        public string ChunkText { get; set; } = string.Empty;
        public int Ordinal { get; set; }
        public string? Section { get; set; }
        public double Score { get; set; }
    }
}
