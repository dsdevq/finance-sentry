namespace FinanceSentry.Modules.BrokerageSync.Domain;

public sealed class IBKRCredential
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public byte[] EncryptedUsername { get; private set; } = [];
    public byte[] UsernameIv { get; private set; } = [];
    public byte[] UsernameAuthTag { get; private set; } = [];

    public byte[] EncryptedPassword { get; private set; } = [];
    public byte[] PasswordIv { get; private set; } = [];
    public byte[] PasswordAuthTag { get; private set; } = [];

    public int KeyVersion { get; private set; }
    public string? AccountId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncError { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private IBKRCredential() { }

    public IBKRCredential(
        Guid userId,
        byte[] encryptedUsername,
        byte[] usernameIv,
        byte[] usernameAuthTag,
        byte[] encryptedPassword,
        byte[] passwordIv,
        byte[] passwordAuthTag,
        int keyVersion,
        string? accountId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EncryptedUsername = encryptedUsername;
        UsernameIv = usernameIv;
        UsernameAuthTag = usernameAuthTag;
        EncryptedPassword = encryptedPassword;
        PasswordIv = passwordIv;
        PasswordAuthTag = passwordAuthTag;
        KeyVersion = keyVersion;
        AccountId = accountId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAccountId(string accountId) => AccountId = accountId;

    public void RecordSyncSuccess()
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = null;
    }

    public void RecordSyncError(string error) => LastSyncError = error;

    public void Deactivate() => IsActive = false;
}
