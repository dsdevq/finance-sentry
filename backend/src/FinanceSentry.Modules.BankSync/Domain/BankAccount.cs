namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

public class BankAccount : Entity
{
    public Guid UserId { get; set; }

    public string ExternalAccountId { get; set; } = string.Empty;

    public string Provider { get; set; } = "plaid";

    public Guid? MonobankCredentialId { get; set; }

    public string BankName { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public string AccountNumberLast4 { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Currency { get; set; } = "EUR";

    public decimal? CurrentBalance { get; set; }

    public string SyncStatus { get; set; } = "pending";

    public string? LastSyncError { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<SyncJob> SyncJobs { get; set; } = [];
    public EncryptedCredential? EncryptedCredential { get; set; }
    public MonobankCredential? MonobankCredential { get; set; }

    public BankAccount() { }

    public BankAccount(Guid userId, string externalAccountId, string bankName, string accountType,
        string accountNumberLast4, string ownerName, string currency, Guid createdBy,
        string provider = "plaid")
    {
        if (string.IsNullOrWhiteSpace(externalAccountId))
            throw new ArgumentException("ExternalAccountId cannot be empty.", nameof(externalAccountId));
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("BankName cannot be empty.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(accountType))
            throw new ArgumentException("AccountType cannot be empty.", nameof(accountType));
        if (accountNumberLast4.Length != 4 || !accountNumberLast4.All(char.IsDigit))
            throw new ArgumentException("AccountNumberLast4 must be exactly 4 digits.", nameof(accountNumberLast4));

        UserId = userId;
        ExternalAccountId = externalAccountId;
        Provider = provider;
        BankName = bankName;
        AccountType = accountType;
        AccountNumberLast4 = accountNumberLast4;
        OwnerName = ownerName;
        Currency = currency.ToUpperInvariant();
        CreatedBy = createdBy;
        SyncStatus = "pending";
    }

    public void MarkActive(decimal balance)
    {
        if (SyncStatus != "syncing")
            throw new InvalidOperationException(
                $"Cannot mark account active from status '{SyncStatus}'.");
        SyncStatus = "active";
        CurrentBalance = balance;
        LastSyncError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string? errorCode = null)
    {
        if (SyncStatus != "syncing")
            throw new InvalidOperationException(
                $"Cannot mark account failed from status '{SyncStatus}'.");
        SyncStatus = "failed";
        LastSyncError = errorCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void BeginSync()
    {
        if (SyncStatus == "syncing")
            throw new InvalidOperationException($"Account {Id} is already syncing.");
        SyncStatus = "syncing";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReauthRequired()
    {
        SyncStatus = "reauth_required";
        UpdatedAt = DateTime.UtcNow;
    }

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(ExternalAccountId))
            throw new ArgumentException("ExternalAccountId cannot be empty");
        if (string.IsNullOrWhiteSpace(BankName))
            throw new ArgumentException("BankName cannot be empty");
        if (string.IsNullOrWhiteSpace(AccountType))
            throw new ArgumentException("AccountType cannot be empty");
        if (AccountNumberLast4.Length != 4)
            throw new ArgumentException("AccountNumberLast4 must be exactly 4 characters");
    }
}
