namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ResearchRetriever(
    IResearchRetrievalRepository retrieval,
    IEmbeddingService embeddings,
    IOptions<ResearchRetrievalOptions> options,
    ILogger<ResearchRetriever> logger) : IResearchRetriever
{
    private const double TitleMatchBonus = 0.2;
    private const int MinTermLength = 2;

    public async Task<ResearchRetrievalResult> SearchAsync(
        ResearchRetrievalRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new ResearchRetrievalResult([], 0);
        }

        var filter = new ResearchRetrievalFilter(
            request.UserId, request.Tickers, request.ThesisId, request.SourceTypes, request.From, request.To);
        var candidates = await retrieval.ListCandidatesAsync(filter, ct);
        if (candidates.Count == 0)
        {
            return new ResearchRetrievalResult([], 0);
        }

        var queryVector = await TryEmbedQueryAsync(request.Query, ct);
        var terms = Tokenize(request.Query);
        var weight = Math.Clamp(options.Value.SemanticWeight, 0, 1);

        var scored = new List<ResearchRetrievalHit>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var lexical = LexicalScore(terms, candidate.Chunk.Text, candidate.Document.Title);
            var semantic = queryVector is not null && candidate.Embedding is not null
                ? CosineSimilarity(queryVector, candidate.Embedding.Vector)
                : 0d;
            var combined = queryVector is not null && candidate.Embedding is not null
                ? (weight * semantic) + ((1 - weight) * lexical)
                : lexical;
            if (combined <= 0)
            {
                continue;
            }

            scored.Add(new ResearchRetrievalHit(
                candidate.Document, candidate.Chunk, Round(semantic), Round(lexical), Round(combined)));
        }

        var ranked = scored
            .OrderByDescending(h => h.CombinedScore)
            .ThenByDescending(h => h.Document.PublishedAt ?? h.Document.CapturedAt)
            .ToList();
        return new ResearchRetrievalResult(ranked.Take(request.Limit).ToList(), ranked.Count);
    }

    private async Task<float[]?> TryEmbedQueryAsync(string query, CancellationToken ct)
    {
        if (!embeddings.IsEnabled)
        {
            return null;
        }

        try
        {
            var vectors = await embeddings.EmbedAsync([query], ct);
            return vectors.Count > 0 ? vectors[0] : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Query embedding failed; falling back to lexical-only ranking");
            return null;
        }
    }

    internal static IReadOnlyList<string> Tokenize(string text)
        => text.ToLowerInvariant()
            .Split([' ', '\t', '\n', ',', '.', ';', ':', '!', '?', '(', ')', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= MinTermLength)
            .Distinct()
            .ToList();

    /// <summary>Fraction of query terms present in the chunk, with a bonus when the title also matches.</summary>
    internal static double LexicalScore(IReadOnlyList<string> terms, string chunkText, string title)
    {
        if (terms.Count == 0)
        {
            return 0;
        }

        var haystack = chunkText.ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();
        var matched = terms.Count(haystack.Contains);
        var titleMatched = terms.Count(titleLower.Contains);
        var score = (double)matched / terms.Count;
        if (titleMatched > 0)
        {
            score += TitleMatchBonus * titleMatched / terms.Count;
        }

        return Math.Min(score, 1);
    }

    internal static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length)
        {
            return 0;
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            magA += (double)a[i] * a[i];
            magB += (double)b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0;
        }

        // Clamp to [0, 1]: negative similarity carries no ranking value for retrieval.
        return Math.Max(0, dot / (Math.Sqrt(magA) * Math.Sqrt(magB)));
    }

    private static double Round(double value) => Math.Round(value, 4);
}
