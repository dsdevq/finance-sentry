namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record GetWatchlistQuery(Guid UserId) : IQuery<IReadOnlyList<WatchlistItemDto>>;

public class GetWatchlistQueryHandler(IWatchlistRepository repo)
    : IQueryHandler<GetWatchlistQuery, IReadOnlyList<WatchlistItemDto>>
{
    public async Task<IReadOnlyList<WatchlistItemDto>> Handle(GetWatchlistQuery query, CancellationToken ct)
    {
        var items = await repo.ListAsync(query.UserId, ct);
        return items
            .Select(w => new WatchlistItemDto(w.Id, w.Ticker, w.Exchange, w.Note, w.AddedAt))
            .ToList();
    }
}
