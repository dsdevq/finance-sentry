namespace FinanceSentry.Infrastructure.Encryption;

/// <summary>
/// Configuration for the AES-256-GCM credential encryption service.
/// Bind from appsettings.json section "Encryption" or environment variables.
///
/// SECURITY: Never commit actual key values to source control.
/// Use environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager).
/// </summary>
public class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>
    /// The key version currently used for new encryptions.
    /// When rotating keys: add the new key to Keys, bump CurrentKeyVersion.
    /// Old encrypted records retain their version and can still be decrypted.
    /// </summary>
    public int CurrentKeyVersion { get; set; } = 1;

    /// <summary>
    /// Dictionary of version → Base64-encoded 32-byte AES-256 key.
    /// Keys must be exactly 32 bytes (256 bits) when decoded from Base64.
    /// </summary>
    public Dictionary<int, string> Keys { get; set; } = [];
}
