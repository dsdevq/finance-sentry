namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Services;

public record GetQuotesQuery(IReadOnlyList<string> Tickers) : IQuery<IReadOnlyList<QuoteDto>>;

public class GetQuotesQueryHandler(IMarketDataService market)
    : IQueryHandler<GetQuotesQuery, IReadOnlyList<QuoteDto>>
{
    public async Task<IReadOnlyList<QuoteDto>> Handle(GetQuotesQuery query, CancellationToken ct)
    {
        var quotes = await market.GetQuotesAsync(query.Tickers, ct);
        return quotes.Values
            .Select(q =>
            {
                var changePct = q.PreviousClose is > 0
                    ? (q.Price - q.PreviousClose.Value) / q.PreviousClose.Value * 100m
                    : (decimal?)null;
                return new QuoteDto(q.Ticker, q.Price, q.PreviousClose, changePct, q.Currency, q.FetchedAt);
            })
            .ToList();
    }
}
