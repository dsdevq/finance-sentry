namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record SearchMarketNewsQuery(
    string? Query,
    IReadOnlyList<string>? Tickers,
    DateTimeOffset? Since,
    int Limit) : IQuery<IReadOnlyList<NewsArticleDto>>;

public class SearchMarketNewsQueryHandler(INewsRepository repo)
    : IQueryHandler<SearchMarketNewsQuery, IReadOnlyList<NewsArticleDto>>
{
    public async Task<IReadOnlyList<NewsArticleDto>> Handle(SearchMarketNewsQuery query, CancellationToken ct)
    {
        var items = await repo.SearchAsync(
            query.Query,
            query.Tickers,
            query.Since,
            query.Limit <= 0 ? 25 : query.Limit,
            ct);

        return items
            .Select(a => new NewsArticleDto(
                a.Id, a.Source, a.Title, a.Url, a.Summary,
                a.Tickers, a.Categories, a.PublishedAt))
            .ToList();
    }
}
