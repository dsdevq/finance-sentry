namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

public class MonobankCredential : Entity
{
    public Guid UserId { get; private set; }
    public byte[] EncryptedToken { get; private set; } = [];
    public byte[] Iv { get; private set; } = [];
    public byte[] AuthTag { get; private set; } = [];
    public int KeyVersion { get; private set; } = 1;
    public DateTime? LastSyncAt { get; set; }

    public ICollection<BankAccount> BankAccounts { get; set; } = [];

    public MonobankCredential() { }

    public MonobankCredential(Guid userId, byte[] encryptedToken, byte[] iv, byte[] authTag)
    {
        UserId = userId;
        EncryptedToken = encryptedToken;
        Iv = iv;
        AuthTag = authTag;
    }
}
