namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Exceptions;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record AddWatchlistItemCommand(
    Guid UserId,
    string Ticker,
    string? Exchange,
    string? Note) : ICommand<WatchlistItemDto>;

public class AddWatchlistItemCommandHandler(IWatchlistRepository repo)
    : ICommandHandler<AddWatchlistItemCommand, WatchlistItemDto>
{
    public async Task<WatchlistItemDto> Handle(AddWatchlistItemCommand cmd, CancellationToken ct)
    {
        var ticker = cmd.Ticker.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(ticker))
        {
            throw new WatchlistItemNotFoundException();
        }

        var existing = await repo.FindAsync(cmd.UserId, ticker, ct);
        if (existing is not null)
        {
            throw new WatchlistItemAlreadyExistsException();
        }

        var item = new WatchlistItem
        {
            UserId = cmd.UserId,
            Ticker = ticker,
            Exchange = cmd.Exchange?.Trim(),
            Note = cmd.Note?.Trim(),
        };

        await repo.AddAsync(item, ct);
        return new WatchlistItemDto(item.Id, item.Ticker, item.Exchange, item.Note, item.AddedAt);
    }
}
