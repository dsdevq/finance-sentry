namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

/// <summary>
/// Domain entity representing a sync job attempt.
/// Tracks sync operation history and status for monitoring.
/// </summary>
public class SyncJob : Entity
{
    /// <summary>
    /// Foreign key to BankAccount.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Current status: pending, running, success, failed.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// When the sync job started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the sync job completed (null if still running).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if sync failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Plaid error code (e.g., ITEM_LOGIN_REQUIRED, RATE_LIMIT_EXCEEDED).
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// How many transactions were successfully synced.
    /// </summary>
    public int TransactionsSynced { get; set; }

    /// <summary>
    /// Date of the last transaction synced during this job.
    /// </summary>
    public DateTime? LastTransactionDate { get; set; }

    /// <summary>
    /// Navigation property to parent account.
    /// </summary>
    public BankAccount? Account { get; set; }

    /// <summary>
    /// Constructor for EF Core.
    /// </summary>
    public SyncJob()
    {
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for creating new sync job.
    /// </summary>
    public SyncJob(Guid accountId) : this()
    {
        AccountId = accountId;
    }

    /// <summary>
    /// Mark sync job as completed successfully.
    /// </summary>
    public void MarkSuccess(int transactionsSynced, DateTime? lastTransactionDate = null)
    {
        Status = "success";
        CompletedAt = DateTime.UtcNow;
        TransactionsSynced = transactionsSynced;
        LastTransactionDate = lastTransactionDate;
        ErrorMessage = null;
        ErrorCode = null;
    }

    /// <summary>
    /// Mark sync job as failed.
    /// </summary>
    public void MarkFailed(string errorMessage, string? errorCode = null)
    {
        Status = "failed";
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Mark sync job as running.
    /// </summary>
    public void MarkRunning()
    {
        Status = "running";
    }

    /// <summary>
    /// Calculate duration of sync job.
    /// </summary>
    public TimeSpan GetDuration()
    {
        var end = CompletedAt ?? DateTime.UtcNow;
        return end - StartedAt;
    }
}
