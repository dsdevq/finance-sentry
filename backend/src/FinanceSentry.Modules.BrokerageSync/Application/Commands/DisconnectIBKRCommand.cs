using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record DisconnectIBKRCommand(Guid UserId) : ICommand<Unit>;

public sealed class DisconnectIBKRCommandHandler(
    IIBKRCredentialRepository credentialRepository,
    IBrokerageHoldingRepository holdingRepository,
    IIBeamContainerManager containerManager,
    ILogger<DisconnectIBKRCommandHandler> logger)
    : ICommandHandler<DisconnectIBKRCommand, Unit>
{
    public async Task<Unit> Handle(DisconnectIBKRCommand command, CancellationToken cancellationToken)
    {
        var credential = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var holdings = await holdingRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        var hasActiveCredential = credential is not null && credential.IsActive;
        if (!hasActiveCredential && holdings.Count == 0)
            throw new BrokerAccountNotFoundException(
                $"No active IBKR account found for user {command.UserId}.");

        if (credential is not null)
        {
            try
            {
                await containerManager.StopAndRemoveAsync(credential.Id, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Best-effort: if Docker is unreachable or the container was already
                // torn down out-of-band, we still need to release the credential row
                // so the user can reconnect. The reconciler will clean up any leaked
                // container on the next sweep.
                logger.LogWarning(
                    ex,
                    "Failed to stop IBeam container for credential {CredentialId}; proceeding with disconnect anyway",
                    credential.Id);
            }

            credential.Deactivate();
            credentialRepository.Update(credential);
        }

        await holdingRepository.DeleteByUserIdAsync(command.UserId, cancellationToken);
        await holdingRepository.SaveChangesAsync(cancellationToken);

        if (credential is not null)
            await credentialRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
