namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using Microsoft.Extensions.Options;

public record SearchResearchCorpusQuery(
    Guid UserId,
    string Query,
    IReadOnlyList<string> Tickers,
    Guid? ThesisId,
    IReadOnlyList<ResearchDocumentSourceType> SourceTypes,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? Limit) : IQuery<ResearchSearchResultDto>;

public class SearchResearchCorpusQueryHandler(
    IResearchRetriever retriever,
    IOptions<ResearchRetrievalOptions> options)
    : IQueryHandler<SearchResearchCorpusQuery, ResearchSearchResultDto>
{
    private const int SnippetLength = 300;

    public async Task<ResearchSearchResultDto> Handle(SearchResearchCorpusQuery query, CancellationToken ct)
    {
        var opts = options.Value;
        var limit = Math.Clamp(query.Limit ?? opts.DefaultSearchLimit, 1, opts.MaxSearchLimit);
        var result = await retriever.SearchAsync(
            new ResearchRetrievalRequest(
                query.UserId,
                query.Query,
                query.Tickers,
                query.ThesisId,
                query.SourceTypes,
                query.From,
                query.To,
                limit),
            ct);

        var hits = result.Hits.Select(h => new ResearchSearchHitDto(
            h.Document.Id,
            h.Chunk.Id,
            h.Document.SourceType.ToString(),
            h.Document.SourceName,
            h.Document.Title,
            h.Document.CanonicalUrl,
            h.Document.PublishedAt,
            h.Document.CapturedAt,
            h.Document.Tickers,
            h.Document.ThesisIds,
            Snippet(h.Chunk.Text),
            h.SemanticScore,
            h.LexicalScore,
            h.CombinedScore)).ToList();

        return new ResearchSearchResultDto(query.Query, hits, DateTimeOffset.UtcNow);
    }

    private static string Snippet(string text)
        => text.Length <= SnippetLength ? text : $"{text[..SnippetLength].TrimEnd()}…";
}
