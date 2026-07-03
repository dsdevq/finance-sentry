using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

public sealed class IBKRConnector(
    IIBKRCredentialRepository credentialRepository,
    ICredentialEncryptionService encryption,
    IIBeamContainerManager containerManager,
    ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult> syncHandler,
    ILogger<IBKRConnector> logger) : IIBKRConnector
{
    private static readonly TimeSpan RollbackTimeout = TimeSpan.FromSeconds(30);

    public async Task<ConnectIBKRResult> ConnectAsync(
        Guid userId,
        string username,
        string password,
        CancellationToken ct)
    {
        var existing = await credentialRepository.GetByUserIdAsync(userId, ct);
        if (existing is not null && existing.IsActive)
        {
            throw new IBKRConnectException(
                "IBKR_DUPLICATE",
                StatusCodes.Status409Conflict,
                "An IBKR account is already connected for this user.");
        }

        var encryptedUsername = encryption.Encrypt(username);
        var encryptedPassword = encryption.Encrypt(password);

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
                userId,
                encryptedUsername.Ciphertext,
                encryptedUsername.Iv,
                encryptedUsername.AuthTag,
                encryptedPassword.Ciphertext,
                encryptedPassword.Iv,
                encryptedPassword.AuthTag,
                encryptedUsername.KeyVersion);
            await credentialRepository.AddAsync(credential, ct);
        }
        await credentialRepository.SaveChangesAsync(ct);

        try
        {
            await containerManager.SpawnAsync(credential.Id, username, password, ct);

            var authenticated = await containerManager.WaitForAuthAsync(credential.Id, ct);
            if (!authenticated)
            {
                throw new IBKRConnectException(
                    "IBKR_INVALID_CREDENTIALS",
                    StatusCodes.Status401Unauthorized,
                    "IBKR gateway did not authenticate within the configured timeout. "
                        + "Check the credentials, and if 2FA is enabled tap approve in your "
                        + "IBKR mobile app within 90 seconds of clicking Connect.");
            }

            var syncResult = await syncHandler.Handle(
                new SyncIBKRHoldingsCommand(userId), ct);

            return new ConnectIBKRResult(
                syncResult.HoldingsCount,
                syncResult.SyncedAt,
                credential.AccountId ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(credential);
            throw;
        }
        catch (IBKRConnectException)
        {
            await RollbackAsync(credential);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IBKR connect failed for user {UserId}", userId);
            await RollbackAsync(credential);
            throw new IBKRConnectException(MapErrorCode(ex), MapStatusCode(ex), ex.Message);
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

    private static int MapStatusCode(Exception ex) => ex switch
    {
        BrokerAuthException => StatusCodes.Status401Unauthorized,
        BrokerAlreadyConnectedException => StatusCodes.Status409Conflict,
        BrokerAccountNotFoundException => StatusCodes.Status404NotFound,
        HttpRequestException => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };
}
