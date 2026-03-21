namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

/// <summary>
/// Domain entity representing a user's connected bank account.
/// One row per institution/account pair.
/// </summary>
public class BankAccount : Entity
{
    /// <summary>
    /// Foreign key to User (stored separately, not in scope).
    /// Enables user-scoped queries.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Opaque token from Plaid (safe to store; never raw credentials).
    /// UNIQUE constraint enforced at database level.
    /// </summary>
    public string PlaidItemId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable bank name (e.g., "AIB Ireland", "Monobank").
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Account type: checking, savings, credit, or investment.
    /// </summary>
    public string AccountType { get; set; } = string.Empty;

    /// <summary>
    /// Last 4 digits of account number only (PCI compliance).
    /// </summary>
    public string AccountNumberLast4 { get; set; } = string.Empty;

    /// <summary>
    /// Account holder name as returned from bank.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// ISO 4217 currency code (EUR, GBP, UAH, USD).
    /// Immutable per account.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Current account balance from Plaid (updated during sync).
    /// </summary>
    public decimal? CurrentBalance { get; set; }

    /// <summary>
    /// Sync status: pending, syncing, active, failed, reauth_required.
    /// </summary>
    public string SyncStatus { get; set; } = "pending";

    /// <summary>
    /// Soft delete flag for audit trail preservation.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID of who created this account.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// User ID of who last modified this account.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<SyncJob> SyncJobs { get; set; } = new List<SyncJob>();
    public EncryptedCredential? EncryptedCredential { get; set; }

    /// <summary>
    /// Constructor for EF Core.
    /// </summary>
    public BankAccount()
    {
    }

    /// <summary>
    /// Constructor for creating new account.
    /// </summary>
    public BankAccount(Guid userId, string plaidItemId, string bankName, string accountType,
        string accountNumberLast4, string ownerName, string currency, Guid createdBy)
    {
        UserId = userId;
        PlaidItemId = plaidItemId;
        BankName = bankName;
        AccountType = accountType;
        AccountNumberLast4 = accountNumberLast4;
        OwnerName = ownerName;
        Currency = currency;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Validates invariants required for account integrity.
    /// </summary>
    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(PlaidItemId))
            throw new ArgumentException("PlaidItemId cannot be empty");
        if (string.IsNullOrWhiteSpace(BankName))
            throw new ArgumentException("BankName cannot be empty");
        if (string.IsNullOrWhiteSpace(AccountType))
            throw new ArgumentException("AccountType cannot be empty");
        if (AccountNumberLast4.Length != 4)
            throw new ArgumentException("AccountNumberLast4 must be exactly 4 characters");
    }
}
