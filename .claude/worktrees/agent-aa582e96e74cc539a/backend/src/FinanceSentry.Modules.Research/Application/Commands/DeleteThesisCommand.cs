namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record DeleteThesisCommand(Guid UserId, Guid Id) : ICommand<bool>;

public class DeleteThesisCommandHandler(IThesisRepository repo)
    : ICommandHandler<DeleteThesisCommand, bool>
{
    public Task<bool> Handle(DeleteThesisCommand cmd, CancellationToken ct)
        => repo.DeleteAsync(cmd.UserId, cmd.Id, ct);
}
