namespace FinanceSentry.Modules.BankSync.Domain.Repositories;

using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Repository interface for BankAccount aggregate root operations.
/// </summary>
public interface IBankAccountRepository
{
    /// <summary>
    /// Add a new bank account.
    /// </summary>
    Task<BankAccount> AddAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get bank account by ID.
    /// </summary>
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get bank account by Plaid item ID.
    /// </summary>
    Task<BankAccount?> GetByPlaidItemIdAsync(string plaidItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all accounts for a user.
    /// </summary>
    Task<IEnumerable<BankAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing bank account.
    /// </summary>
    Task<BankAccount> UpdateAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete bank account (soft delete by setting IsActive = false).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get accounts with specific sync status.
    /// </summary>
    Task<IEnumerable<BankAccount>> GetBySyncStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active (IsActive=true) accounts regardless of sync status. Used by the scheduler.
    /// </summary>
    Task<IEnumerable<BankAccount>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Transaction operations.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Add a new transaction.
    /// </summary>
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple transactions in batch.
    /// </summary>
    Task<IEnumerable<Transaction>> AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transaction by ID.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all transactions for an account.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions by account ID with pagination.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions posted after a specific date.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByAccountIdAndDateAsync(Guid accountId, DateTime desde, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if transaction with unique hash already exists for account.
    /// </summary>
    Task<bool> ExistsByUniqueHashAsync(Guid accountId, string uniqueHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transaction count for an account.
    /// </summary>
    Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes all transactions for an account (sets IsActive=false) for account removal flow.
    /// Uses IgnoreQueryFilters() internally to find already-inactive rows (idempotent).
    /// </summary>
    Task SoftDeleteByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for SyncJob operations.
/// </summary>
public interface ISyncJobRepository
{
    /// <summary>
    /// Add a new sync job.
    /// </summary>
    Task<SyncJob> AddAsync(SyncJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sync job by ID.
    /// </summary>
    Task<SyncJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all sync jobs for an account.
    /// </summary>
    Task<IEnumerable<SyncJob>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most recent sync job for an account.
    /// </summary>
    Task<SyncJob?> GetLatestByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sync jobs with specific status.
    /// </summary>
    Task<IEnumerable<SyncJob>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update sync job.
    /// </summary>
    Task<SyncJob> UpdateAsync(SyncJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete sync job (hard delete, safe for job records).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if there is at least one SyncJob with the given status for the account.
    /// Used to check for a currently running job before starting a new one.
    /// </summary>
    Task<bool> HasRunningJobAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for EncryptedCredential operations.
/// </summary>
public interface IEncryptedCredentialRepository
{
    /// <summary>
    /// Add new encrypted credential.
    /// </summary>
    Task<EncryptedCredential> AddAsync(EncryptedCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get encrypted credential by ID.
    /// </summary>
    Task<EncryptedCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get encrypted credential by account ID (one-to-one relationship).
    /// </summary>
    Task<EncryptedCredential?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update encrypted credential (e.g., for key rotation).
    /// </summary>
    Task<EncryptedCredential> UpdateAsync(EncryptedCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete encrypted credential.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
