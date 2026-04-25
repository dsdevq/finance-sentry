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
        if (credential is null || !credential.IsActive)
        {
            throw new BinanceAccountNotFoundException();
        }

        credential.Deactivate();
        credentialRepository.Update(credential);

        await holdingRepository.DeleteByUserIdAsync(command.UserId, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
