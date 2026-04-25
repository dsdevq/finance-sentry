using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record DisconnectIBKRCommand(Guid UserId) : ICommand<Unit>;

public sealed class DisconnectIBKRCommandHandler(
    IIBKRCredentialRepository credentialRepository,
    IBrokerageHoldingRepository holdingRepository)
    : ICommandHandler<DisconnectIBKRCommand, Unit>
{
    public async Task<Unit> Handle(DisconnectIBKRCommand command, CancellationToken cancellationToken)
    {
        var credential = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken)
            ?? throw new BrokerAccountNotFoundException(
                $"No active IBKR account found for user {command.UserId}.");

        credential.Deactivate();
        credentialRepository.Update(credential);

        await holdingRepository.DeleteByUserIdAsync(command.UserId, cancellationToken);
        await holdingRepository.SaveChangesAsync(cancellationToken);

        await credentialRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
