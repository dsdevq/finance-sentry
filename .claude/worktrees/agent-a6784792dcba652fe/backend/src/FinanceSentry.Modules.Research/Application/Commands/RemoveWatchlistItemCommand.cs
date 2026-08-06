namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record RemoveWatchlistItemCommand(Guid UserId, Guid ItemId) : ICommand<bool>;

public class RemoveWatchlistItemCommandHandler(IWatchlistRepository repo)
    : ICommandHandler<RemoveWatchlistItemCommand, bool>
{
    public Task<bool> Handle(RemoveWatchlistItemCommand cmd, CancellationToken ct)
        => repo.RemoveAsync(cmd.UserId, cmd.ItemId, ct);
}
