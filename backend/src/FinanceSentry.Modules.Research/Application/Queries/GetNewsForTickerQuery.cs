namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record GetNewsForTickerQuery(
    string Ticker,
    DateTimeOffset? Since,
    int Limit) : IQuery<IReadOnlyList<NewsArticleDto>>;

public class GetNewsForTickerQueryHandler(INewsRepository repo)
    : IQueryHandler<GetNewsForTickerQuery, IReadOnlyList<NewsArticleDto>>
{
    public async Task<IReadOnlyList<NewsArticleDto>> Handle(GetNewsForTickerQuery query, CancellationToken ct)
    {
        var items = await repo.GetForTickerAsync(
            query.Ticker,
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
