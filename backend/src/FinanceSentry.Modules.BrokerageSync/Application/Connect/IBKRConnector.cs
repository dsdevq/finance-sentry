using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

public sealed class IBKRConnector(
    IIBKRCredentialRepository credentialRepository,
    ICredentialEncryptionService encryption,
    ILogger<IBKRConnector> logger) : IIBKRConnector
{
    public async Task<ConnectIBKRResult> ConnectAsync(
        Guid userId,
        ConnectIBKRArtifacts artifacts,
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

        var secret = encryption.Encrypt(artifacts.AccessTokenSecret);
        var signatureKey = encryption.Encrypt(artifacts.SignatureKey);
        var encryptionKey = encryption.Encrypt(artifacts.EncryptionKey);

        if (existing is not null)
        {
            existing.Reactivate(
                artifacts.ConsumerKey,
                artifacts.AccessToken,
                artifacts.DhParam,
                secret.Ciphertext, secret.Iv, secret.AuthTag,
                signatureKey.Ciphertext, signatureKey.Iv, signatureKey.AuthTag,
                encryptionKey.Ciphertext, encryptionKey.Iv, encryptionKey.AuthTag,
                secret.KeyVersion);
            credentialRepository.Update(existing);
        }
        else
        {
            var credential = new IBKRCredential(
                userId,
                artifacts.ConsumerKey,
                artifacts.AccessToken,
                artifacts.DhParam,
                secret.Ciphertext, secret.Iv, secret.AuthTag,
                signatureKey.Ciphertext, signatureKey.Iv, signatureKey.AuthTag,
                encryptionKey.Ciphertext, encryptionKey.Iv, encryptionKey.AuthTag,
                secret.KeyVersion);
            await credentialRepository.AddAsync(credential, ct);
        }

        await credentialRepository.SaveChangesAsync(ct);
        logger.LogInformation("IBKR OAuth artifacts persisted for user {UserId}", userId);

        // Live-session-token derivation and the initial holdings sync land with
        // the OAuth signing client. Until the consumer key activates on IBKR's
        // side (weekend server restart), connect only persists the artifacts.
        return new ConnectIBKRResult(0, DateTime.UtcNow, string.Empty);
    }
}
