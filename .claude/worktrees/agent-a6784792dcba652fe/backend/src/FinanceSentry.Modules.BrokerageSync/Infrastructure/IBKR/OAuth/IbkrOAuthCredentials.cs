using System.Numerics;
using System.Security.Cryptography;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR.OAuth;

/// <summary>
/// A user's decrypted, ready-to-use OAuth 1.0a material. Owns the two parsed RSA
/// keys, so callers must dispose it. Never logged.
/// </summary>
public sealed class IbkrOAuthCredentials : IDisposable
{
    public required Guid UserId { get; init; }
    public required string ConsumerKey { get; init; }
    public required string AccessToken { get; init; }

    /// <summary>The IBKR-issued access token secret (base64, still RSA-encrypted with the user's public encryption key).</summary>
    public required string AccessTokenSecret { get; init; }

    public required RSA SignatureKey { get; init; }
    public required RSA EncryptionKey { get; init; }
    public required BigInteger DhPrime { get; init; }
    public required BigInteger DhGenerator { get; init; }

    public void Dispose()
    {
        SignatureKey.Dispose();
        EncryptionKey.Dispose();
    }
}
