using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record ConnectIBKRCommand(
    Guid UserId,
    string Username,
    string Password) : ICommand<ConnectIBKRResult>;

public sealed record ConnectIBKRResult(int HoldingsCount, DateTime ConnectedAt);

public sealed class ConnectIBKRCommandHandler(
    IIBKRCredentialRepository credentialRepository,
    IBrokerAdapter adapter,
    ICredentialEncryptionService encryption,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler)
    : ICommandHandler<ConnectIBKRCommand, ConnectIBKRResult>
{
    public async Task<ConnectIBKRResult> Handle(ConnectIBKRCommand command, CancellationToken cancellationToken)
    {
        var existing = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (existing is not null && existing.IsActive)
            throw new BrokerAlreadyConnectedException(
                "An IBKR account is already connected for this user.");

        await adapter.AuthenticateAsync(command.Username, command.Password, cancellationToken);

        var accountId = await adapter.GetAccountIdAsync(cancellationToken);

        var encUsername = encryption.Encrypt(command.Username);
        var encPassword = encryption.Encrypt(command.Password);

        var credential = new IBKRCredential(
            command.UserId,
            encUsername.Ciphertext, encUsername.Iv, encUsername.AuthTag,
            encPassword.Ciphertext, encPassword.Iv, encPassword.AuthTag,
            encUsername.KeyVersion,
            accountId);

        await credentialRepository.AddAsync(credential, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);

        var syncResult = await syncHandler.Handle(new SyncIBKRHoldingsCommand(command.UserId), cancellationToken);

        return new ConnectIBKRResult(syncResult.HoldingsCount, syncResult.SyncedAt);
    }
}
