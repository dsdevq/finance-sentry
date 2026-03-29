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
    /// Last sync error code (e.g., "INSTITUTION_NOT_RESPONDING"). Cleared on successful sync.
    /// </summary>
    public string? LastSyncError { get; set; }

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
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<SyncJob> SyncJobs { get; set; } = [];
    public EncryptedCredential? EncryptedCredential { get; set; }

    /// <summary>
    /// Constructor for EF Core.
    /// </summary>
    public BankAccount()
    {
    }

    /// <summary>
    /// Constructor for creating new account. Validates all required invariants eagerly.
    /// </summary>
    public BankAccount(Guid userId, string plaidItemId, string bankName, string accountType,
        string accountNumberLast4, string ownerName, string currency, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(plaidItemId))
            throw new ArgumentException("PlaidItemId cannot be empty.", nameof(plaidItemId));
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("BankName cannot be empty.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(accountType))
            throw new ArgumentException("AccountType cannot be empty.", nameof(accountType));
        if (accountNumberLast4.Length != 4 || !accountNumberLast4.All(char.IsDigit))
            throw new ArgumentException("AccountNumberLast4 must be exactly 4 digits.", nameof(accountNumberLast4));

        UserId = userId;
        PlaidItemId = plaidItemId;
        BankName = bankName;
        AccountType = accountType;
        AccountNumberLast4 = accountNumberLast4;
        OwnerName = ownerName;
        Currency = currency.ToUpperInvariant();
        CreatedBy = createdBy;
        SyncStatus = "pending";
    }

    // ── State machine ────────────────────────────────────────────────────────

    /// <summary>Transitions pending → syncing. Throws if already syncing/active/failed.</summary>
    public void StartSync()
    {
        if (SyncStatus != "pending" && SyncStatus != "reauth_required")
            throw new InvalidOperationException(
                $"Cannot start sync from status '{SyncStatus}'. Only 'pending' or 'reauth_required' accounts can begin syncing.");

        SyncStatus = "syncing";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions syncing → active, updates balance.</summary>
    public void MarkActive(decimal balance)
    {
        if (SyncStatus != "syncing")
            throw new InvalidOperationException(
                $"Cannot mark account active from status '{SyncStatus}'. Account must be syncing first.");

        SyncStatus = "active";
        CurrentBalance = balance;
        LastSyncError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions syncing → failed.</summary>
    public void MarkFailed(string? errorCode = null)
    {
        if (SyncStatus != "syncing")
            throw new InvalidOperationException(
                $"Cannot mark account failed from status '{SyncStatus}'. Account must be syncing first.");

        SyncStatus = "failed";
        LastSyncError = errorCode;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions active/failed/pending/reauth_required → syncing for background/scheduled sync.
    /// Unlike StartSync(), this method also accepts accounts that are already active or failed,
    /// enabling re-sync without requiring a full re-auth cycle.
    /// </summary>
    public void BeginSync()
    {
        if (SyncStatus == "syncing")
            throw new InvalidOperationException($"Account {Id} is already syncing.");
        SyncStatus = "syncing";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions any status → reauth_required (Plaid item expired).</summary>
    public void MarkReauthRequired()
    {
        SyncStatus = "reauth_required";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates invariants. Kept for backward compatibility; constructor now validates eagerly.
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
