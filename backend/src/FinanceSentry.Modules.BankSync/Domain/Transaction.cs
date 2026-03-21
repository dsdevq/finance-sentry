namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

/// <summary>
/// Domain entity representing an individual transaction.
/// Immutable after creation. Includes deduplication via unique_hash.
/// </summary>
public class Transaction : Entity
{
    /// <summary>
    /// Foreign key to BankAccount.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Transaction amount (always positive for clarity; direction indicated by type).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date the transaction was actually posted to the account.
    /// Nullable for pending transactions (not yet posted).
    /// </summary>
    public DateTime? PostedDate { get; set; }

    /// <summary>
    /// Date the transaction occurred (authorization date).
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Description or merchant information.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 hash for deduplication.
    /// Hash of: account_id|amount|date|description
    /// Unique constraint at database level per account.
    /// </summary>
    public string UniqueHash { get; set; } = string.Empty;

    /// <summary>
    /// Whether transaction is still pending (not officially posted).
    /// Treat pending + posted as different transactions (different dates).
    /// </summary>
    public bool IsPending { get; set; }

    /// <summary>
    /// Optional transaction type (debit, credit, transfer, etc.).
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Optional merchant or counterparty name.
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Navigation property to parent account.
    /// </summary>
    public BankAccount? Account { get; set; }

    /// <summary>
    /// Constructor for EF Core.
    /// </summary>
    public Transaction()
    {
    }

    /// <summary>
    /// Constructor for creating new transaction.
    /// </summary>
    public Transaction(Guid accountId, decimal amount, DateTime transactionDate,
        string description, string uniqueHash, bool isPending = false)
    {
        AccountId = accountId;
        Amount = amount;
        TransactionDate = transactionDate;
        Description = description;
        UniqueHash = uniqueHash;
        IsPending = isPending;
    }

    /// <summary>
    /// Validates invariants required for transaction integrity.
    /// </summary>
    public void ValidateInvariants()
    {
        if (Amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description cannot be empty");
        if (string.IsNullOrWhiteSpace(UniqueHash))
            throw new ArgumentException("UniqueHash cannot be empty");
    }
}
