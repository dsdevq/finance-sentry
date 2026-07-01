using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed class IBeamReconciler(
    IIBKRCredentialRepository credentialRepository,
    IIBeamContainerManager containerManager,
    ICredentialEncryptionService encryption,
    ILogger<IBeamReconciler> logger) : IIBeamReconciler
{
    public async Task ReconcileAllAsync(CancellationToken ct = default)
    {
        var credentials = await credentialRepository.GetAllActiveAsync(ct);

        foreach (var credential in credentials)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (await containerManager.IsRunningAsync(credential.Id, ct))
                {
                    logger.LogDebug(
                        "IBeam container for credential {CredentialId} already running — skipping.",
                        credential.Id);
                    continue;
                }

                var username = encryption.Decrypt(
                    credential.EncryptedUsername,
                    credential.UsernameIv,
                    credential.UsernameAuthTag,
                    credential.KeyVersion);
                var password = encryption.Decrypt(
                    credential.EncryptedPassword,
                    credential.PasswordIv,
                    credential.PasswordAuthTag,
                    credential.KeyVersion);

                logger.LogInformation(
                    "Reconciling: spawning IBeam container for credential {CredentialId}.",
                    credential.Id);
                await containerManager.SpawnAsync(credential.Id, username, password, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to reconcile IBeam container for credential {CredentialId}.",
                    credential.Id);
            }
        }
    }
}
