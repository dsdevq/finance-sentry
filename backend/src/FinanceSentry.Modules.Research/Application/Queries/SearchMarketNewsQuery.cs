namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record SearchMarketNewsQuery(
    string? Query,
    IReadOnlyList<string>? Tickers,
    Guid? ThesisId,
    DateTimeOffset? Since,
    int Limit) : IQuery<SearchMarketNewsResult>;

public class SearchMarketNewsQueryHandler(INewsRepository repo, INewsSourceRepository sources)
    : IQueryHandler<SearchMarketNewsQuery, SearchMarketNewsResult>
{
    public async Task<SearchMarketNewsResult> Handle(SearchMarketNewsQuery query, CancellationToken ct)
    {
        var items = await repo.SearchAsync(
            query.Query,
            query.Tickers,
            query.ThesisId,
            query.Since,
            query.Limit <= 0 ? 25 : query.Limit,
            ct);

        var articles = items
            .Select(a => new NewsArticleDto(
                a.Id, a.Source, a.Title, a.Url, a.Summary,
                a.Tickers, a.Categories, a.PublishedAt))
            .ToList();

        var health = await GetSourceHealthAsync(query.ThesisId, ct);
        var coverage = health.Any(h => h.ConsecutiveFailures > 0) ? "degraded" : "ok";

        return new SearchMarketNewsResult(articles, health, coverage, DateTimeOffset.UtcNow);
    }

    private async Task<IReadOnlyList<NewsSourceHealthDto>> GetSourceHealthAsync(Guid? thesisId, CancellationToken ct)
    {
        if (thesisId is null)
        {
            return [];
        }

        var all = await sources.ListAllAsync(ct);
        return all
            .Where(s => s.ThesisId == thesisId)
            .Select(s => new NewsSourceHealthDto(
                s.Id,
                s.Name,
                s.Url,
                s.ConsecutiveFailures,
                s.LastSuccessAt,
                s.LastFailureReason,
                s.ConsecutiveFailures switch
                {
                    0 => "ok",
                    1 => "degraded",
                    _ => "down",
                }))
            .ToList();
    }
}
