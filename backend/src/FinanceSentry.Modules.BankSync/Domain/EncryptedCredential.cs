namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

/// <summary>
/// Domain entity for storing encrypted Plaid access tokens.
/// Stores: encrypted_data, IV (initialization vector), auth_tag (authentication tag for GCM).
/// Never decrypts in entity itself—decryption happens in service layer.
/// </summary>
public class EncryptedCredential : Entity
{
    /// <summary>
    /// Foreign key to BankAccount (one-to-one relationship).
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// AES-256-GCM encrypted Plaid access token.
    /// </summary>
    public byte[] EncryptedData { get; set; } = [];

    /// <summary>
    /// Initialization vector (IV) for encryption (typically 12 bytes).
    /// </summary>
    public byte[] Iv { get; set; } = [];

    /// <summary>
    /// Authentication tag from GCM mode (typically 16 bytes).
    /// Used to verify ciphertext integrity.
    /// </summary>
    public byte[] AuthTag { get; set; } = [];

    /// <summary>
    /// Key version for rotation support.
    /// Allows maintaining multiple master keys for transitioning between key versions.
    /// </summary>
    public int KeyVersion { get; set; } = 1;

    /// <summary>
    /// When the credential was last used (for audit trail).
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Navigation property to parent account.
    /// </summary>
    public BankAccount? Account { get; set; }

    /// <summary>
    /// Constructor for EF Core.
    /// </summary>
    public EncryptedCredential()
    {
    }

    /// <summary>
    /// Constructor for creating new encrypted credential.
    /// </summary>
    public EncryptedCredential(Guid accountId, byte[] encryptedData, byte[] iv,
        byte[] authTag, int keyVersion = 1)
    {
        AccountId = accountId;
        EncryptedData = encryptedData;
        Iv = iv;
        AuthTag = authTag;
        KeyVersion = keyVersion;
    }

    /// <summary>
    /// Update last used timestamp (called after successful decryption and use).
    /// </summary>
    public void UpdateLastUsedAt()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates invariants required for credential integrity.
    /// </summary>
    public void ValidateInvariants()
    {
        if (EncryptedData.Length == 0)
            throw new ArgumentException("EncryptedData cannot be empty");
        if (Iv.Length != 12)
            throw new ArgumentException("IV must be exactly 12 bytes");
        if (AuthTag.Length != 16)
            throw new ArgumentException("AuthTag must be exactly 16 bytes");
        if (KeyVersion < 1)
            throw new ArgumentException("KeyVersion must be >= 1");
    }
}
