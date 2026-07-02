using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.Extensions.Logging;

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
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler,
    ILogger<ConnectIBKRCommandHandler> logger)
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

        // Everything below can fail (docker unreachable, IBKR auth timeout, sync
        // error, request cancellation). Any exit must roll the credential back
        // to IsActive=false and best-effort tear down the container; otherwise
        // the row gets stuck active and the container silently retries IBKR
        // login in the background, which trips the account's lockout counter.
        try
        {
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
        catch
        {
            // Fresh, bounded token: if the caller cancelled us, we still need to
            // finish the tear-down. Any leaked container keeps hammering IBKR.
            using var rollbackCts = new CancellationTokenSource(RollbackTimeout);
            await RollbackAsync(credential, rollbackCts.Token);
            throw;
        }
    }

    private static readonly TimeSpan RollbackTimeout = TimeSpan.FromSeconds(30);

    private async Task RollbackAsync(IBKRCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            await containerManager.StopAndRemoveAsync(credential.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Rollback: failed to stop IBeam container for credential {CredentialId}; continuing with credential deactivation",
                credential.Id);
        }

        try
        {
            credential.Deactivate();
            credentialRepository.Update(credential);
            await credentialRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Rollback: failed to deactivate credential {CredentialId} after connect failure. Row may be stuck IsActive=true and require manual intervention.",
                credential.Id);
        }
    }
}
