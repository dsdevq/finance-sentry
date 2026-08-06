using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;

namespace FinanceSentry.Modules.CryptoSync.Application.Commands;

public sealed record DisconnectBinanceCommand(Guid UserId) : ICommand<Unit>;

public sealed class DisconnectBinanceCommandHandler(
    IBinanceCredentialRepository credentialRepository,
    ICryptoHoldingRepository holdingRepository)
    : ICommandHandler<DisconnectBinanceCommand, Unit>
{
    public async Task<Unit> Handle(DisconnectBinanceCommand command, CancellationToken cancellationToken)
    {
        var credential = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var holdings = await holdingRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        var hasActiveCredential = credential is not null && credential.IsActive;
        if (!hasActiveCredential && holdings.Count == 0)
        {
            throw new BinanceAccountNotFoundException();
        }

        if (hasActiveCredential)
        {
            credential!.Deactivate();
            credentialRepository.Update(credential);
        }

        await holdingRepository.DeleteByUserIdAsync(command.UserId, cancellationToken);

        if (hasActiveCredential)
        {
            await credentialRepository.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
