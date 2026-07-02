namespace FinanceSentry.Modules.BrokerageSync.Domain;

/// <summary>
/// Represents a single user's link to an IBKR account.
///
/// Per-user IBeam model: each connected user has their IBKR username + password
/// stored encrypted at rest (AES-256-GCM). On connect, the backend spawns a
/// dedicated IBeam container for the user using the decrypted credentials; the
/// container name is derived from <see cref="Id"/> so the API can address the
/// per-user gateway by DNS on the compose network.
/// </summary>
public sealed class IBKRCredential
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? AccountId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncError { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public byte[] EncryptedUsername { get; private set; } = [];
    public byte[] UsernameIv { get; private set; } = [];
    public byte[] UsernameAuthTag { get; private set; } = [];
    public byte[] EncryptedPassword { get; private set; } = [];
    public byte[] PasswordIv { get; private set; } = [];
    public byte[] PasswordAuthTag { get; private set; } = [];
    public int KeyVersion { get; private set; } = 1;

    private IBKRCredential() { }

    public IBKRCredential(
        Guid userId,
        byte[] encryptedUsername,
        byte[] usernameIv,
        byte[] usernameAuthTag,
        byte[] encryptedPassword,
        byte[] passwordIv,
        byte[] passwordAuthTag,
        int keyVersion)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        EncryptedUsername = encryptedUsername;
        UsernameIv = usernameIv;
        UsernameAuthTag = usernameAuthTag;
        EncryptedPassword = encryptedPassword;
        PasswordIv = passwordIv;
        PasswordAuthTag = passwordAuthTag;
        KeyVersion = keyVersion;
    }

    public void UpdateAccountId(string accountId) => AccountId = accountId;

    public void RecordSyncSuccess()
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = null;
    }

    public void RecordSyncError(string error) => LastSyncError = error;

    public void Deactivate() => IsActive = false;

    public void Reactivate(
        byte[] encryptedUsername,
        byte[] usernameIv,
        byte[] usernameAuthTag,
        byte[] encryptedPassword,
        byte[] passwordIv,
        byte[] passwordAuthTag,
        int keyVersion)
    {
        IsActive = true;
        AccountId = null;
        LastSyncAt = null;
        LastSyncError = null;
        EncryptedUsername = encryptedUsername;
        UsernameIv = usernameIv;
        UsernameAuthTag = usernameAuthTag;
        EncryptedPassword = encryptedPassword;
        PasswordIv = passwordIv;
        PasswordAuthTag = passwordAuthTag;
        KeyVersion = keyVersion;
    }
}
