namespace FinanceSentry.Infrastructure.Encryption;

/// <summary>
/// Encrypts and decrypts sensitive credential data (Plaid access tokens) using AES-256-GCM.
/// All plaintext tokens must be encrypted before storage and decrypted only at point of use.
/// Never log plaintext tokens or encryption keys.
/// </summary>
public interface ICredentialEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string (e.g., Plaid access token) using AES-256-GCM.
    /// Returns the ciphertext, IV, authentication tag, and key version used.
    /// </summary>
    EncryptionResult Encrypt(string plaintext);

    /// <summary>
    /// Decrypts AES-256-GCM ciphertext back to plaintext.
    /// Validates the authentication tag — throws if data has been tampered with.
    /// </summary>
    /// <exception cref="CryptographicException">Thrown when auth tag validation fails (tampered data).</exception>
    string Decrypt(byte[] ciphertext, byte[] iv, byte[] authTag, int keyVersion);
}

/// <summary>
/// Result of an encryption operation. Contains all fields needed to store and later decrypt the data.
/// </summary>
public record EncryptionResult(
    byte[] Ciphertext,
    byte[] Iv,
    byte[] AuthTag,
    int KeyVersion
);
