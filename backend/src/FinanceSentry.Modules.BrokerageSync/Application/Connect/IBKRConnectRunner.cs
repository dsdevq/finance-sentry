using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Executes a single async IBKR connect session end-to-end. Owns the state
/// transitions on <see cref="IIBKRConnectSessionStore"/> and rollback on
/// any exit path (failure, cancellation) so the credential row and IBeam
/// container are never left dangling.
/// </summary>
public sealed class IBKRConnectRunner(
    IIBKRConnectSessionStore sessionStore,
    IIBKRCredentialRepository credentialRepository,
    ICredentialEncryptionService encryption,
    IIBeamContainerManager containerManager,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler,
    ILogger<IBKRConnectRunner> logger)
{
    private static readonly TimeSpan RollbackTimeout = TimeSpan.FromSeconds(30);

    public async Task RunAsync(
        Guid sessionId,
        Guid userId,
        string username,
        string password,
        CancellationToken sessionToken)
    {
        IBKRCredential? credential = null;
        try
        {
            var existing = await credentialRepository.GetByUserIdAsync(userId, sessionToken);
            if (existing is not null && existing.IsActive)
            {
                sessionStore.MarkFailed(
                    sessionId,
                    "IBKR_DUPLICATE",
                    "An IBKR account is already connected for this user.");
                return;
            }

            var encryptedUsername = encryption.Encrypt(username);
            var encryptedPassword = encryption.Encrypt(password);

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
                    userId,
                    encryptedUsername.Ciphertext,
                    encryptedUsername.Iv,
                    encryptedUsername.AuthTag,
                    encryptedPassword.Ciphertext,
                    encryptedPassword.Iv,
                    encryptedPassword.AuthTag,
                    encryptedUsername.KeyVersion);
                await credentialRepository.AddAsync(credential, sessionToken);
            }
            await credentialRepository.SaveChangesAsync(sessionToken);

            sessionStore.TransitionTo(sessionId, IBKRConnectStatus.Spawning);
            await containerManager.SpawnAsync(credential.Id, username, password, sessionToken);

            sessionStore.TransitionTo(sessionId, IBKRConnectStatus.AwaitingAuth);
            var authenticated = await containerManager.WaitForAuthAsync(credential.Id, sessionToken);
            if (!authenticated)
            {
                sessionStore.MarkFailed(
                    sessionId,
                    "IBKR_INVALID_CREDENTIALS",
                    "IBKR gateway did not authenticate within the configured timeout. Check the credentials, and if 2FA is enabled tap approve in your IBKR mobile app within 90 seconds of clicking Connect.");
                await RollbackAsync(credential);
                return;
            }

            sessionStore.TransitionTo(sessionId, IBKRConnectStatus.Syncing);
            var syncResult = await syncHandler.Handle(
                new SyncIBKRHoldingsCommand(userId), sessionToken);

            sessionStore.MarkCompleted(
                sessionId,
                new ConnectIBKRResult(
                    syncResult.HoldingsCount,
                    syncResult.SyncedAt,
                    credential.AccountId ?? string.Empty));
        }
        catch (OperationCanceledException)
        {
            sessionStore.MarkCancelled(sessionId);
            if (credential is not null)
                await RollbackAsync(credential);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IBKR connect runner failed for session {SessionId}", sessionId);
            sessionStore.MarkFailed(sessionId, MapErrorCode(ex), ex.Message);
            if (credential is not null)
                await RollbackAsync(credential);
        }
    }

    private async Task RollbackAsync(IBKRCredential credential)
    {
        using var rollbackCts = new CancellationTokenSource(RollbackTimeout);

        try
        {
            await containerManager.StopAndRemoveAsync(credential.Id, rollbackCts.Token);
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
            await credentialRepository.SaveChangesAsync(rollbackCts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Rollback: failed to deactivate credential {CredentialId}. Row may be stuck IsActive=true and require manual intervention.",
                credential.Id);
        }
    }

    private static string MapErrorCode(Exception ex) => ex switch
    {
        BrokerAuthException => "IBKR_INVALID_CREDENTIALS",
        BrokerAlreadyConnectedException => "IBKR_DUPLICATE",
        BrokerAccountNotFoundException => "NOT_CONNECTED",
        HttpRequestException => "IBKR_GATEWAY_UNAVAILABLE",
        _ => "INTERNAL_ERROR",
    };
}
