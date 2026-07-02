using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

/// <summary>
/// Per-user IBKR connect request. Username and password are encrypted at rest,
/// then used to spawn the user's dedicated IBeam gateway container.
/// </summary>
public sealed record ConnectIBKRRequest(string Username, string Password);

public sealed record ConnectIBKRCommand(Guid UserId, string Username, string Password) : ICommand<ConnectIBKRResult>;

public sealed record ConnectIBKRResult(int HoldingsCount, DateTime ConnectedAt, string AccountId);

public sealed class ConnectIBKRCommandHandler(
    IIBKRCredentialRepository credentialRepository,
    ICredentialEncryptionService encryption,
    IIBeamContainerManager containerManager,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler)
    : ICommandHandler<ConnectIBKRCommand, ConnectIBKRResult>
{
    public async Task<ConnectIBKRResult> Handle(ConnectIBKRCommand command, CancellationToken cancellationToken)
    {
        var existing = await credentialRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (existing is not null && existing.IsActive)
            throw new BrokerAlreadyConnectedException(
                "An IBKR account is already connected for this user.");

        var encryptedUsername = encryption.Encrypt(command.Username);
        var encryptedPassword = encryption.Encrypt(command.Password);

        IBKRCredential credential;
        if (existing is not null)
        {
            existing.Reactivate(
                encryptedUsername.Ciphertext,
                encryptedUsername.Iv,
                encryptedUsername.AuthTag,
                encryptedPassword.Ciphertext,
                encryptedPassword.Iv,
                encryptedPassword.AuthTag,
                encryptedUsername.KeyVersion);
            credentialRepository.Update(existing);
            credential = existing;
        }
        else
        {
            credential = new IBKRCredential(
                command.UserId,
                encryptedUsername.Ciphertext,
                encryptedUsername.Iv,
                encryptedUsername.AuthTag,
                encryptedPassword.Ciphertext,
                encryptedPassword.Iv,
                encryptedPassword.AuthTag,
                encryptedUsername.KeyVersion);
            await credentialRepository.AddAsync(credential, cancellationToken);
        }
        await credentialRepository.SaveChangesAsync(cancellationToken);

        await containerManager.SpawnAsync(
            credential.Id, command.Username, command.Password, cancellationToken);

        var authenticated = await containerManager.WaitForAuthAsync(credential.Id, cancellationToken);
        if (!authenticated)
            throw new BrokerAuthException(
                "IBKR gateway did not authenticate within the configured timeout. Check the credentials and gateway logs.",
                "IBKR");

        var syncResult = await syncHandler.Handle(
            new SyncIBKRHoldingsCommand(command.UserId), cancellationToken);

        return new ConnectIBKRResult(
            syncResult.HoldingsCount,
            syncResult.SyncedAt,
            credential.AccountId ?? string.Empty);
    }
}
