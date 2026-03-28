namespace FinanceSentry.Infrastructure.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Structured logging wrapper for BankSync operations.
///
/// Security rules (FR-003, Constitution V):
/// - Never log plaintext tokens, passwords, or full account numbers.
/// - Always include correlation ID for distributed tracing.
/// - Plaid errors logged with error code + user-safe message only.
/// </summary>
public interface IBankSyncLogger
{
    void SyncStarted(string correlationId, Guid accountId);
    void SyncCompleted(string correlationId, Guid accountId, int transactionsFetched, int deduped, long durationMs);
    void SyncFailed(string correlationId, Guid accountId, string errorCode, string safeMessage, int retryCount);
    void SyncRetrying(string correlationId, Guid accountId, int attempt, TimeSpan delay, string reason);
    void CredentialAccessed(string correlationId, Guid accountId);
    void WebhookReceived(string correlationId, string webhookType, string webhookCode);
}

/// <inheritdoc />
public class BankSyncLogger : IBankSyncLogger
{
    private readonly ILogger<BankSyncLogger> _logger;

    // Log message templates — using compile-time constants for performance
    private static readonly Action<ILogger, string, Guid, Exception?> _syncStarted =
        LoggerMessage.Define<string, Guid>(LogLevel.Information,
            new EventId(1001, "SyncStarted"),
            "[{CorrelationId}] Sync started for account {AccountId}");

    private static readonly Action<ILogger, string, Guid, int, int, long, Exception?> _syncCompleted =
        LoggerMessage.Define<string, Guid, int, int, long>(LogLevel.Information,
            new EventId(1002, "SyncCompleted"),
            "[{CorrelationId}] Sync completed for account {AccountId}: " +
            "{TransactionsFetched} fetched, {Deduped} deduped, {DurationMs}ms");

    private static readonly Action<ILogger, string, Guid, string, string, int, Exception?> _syncFailed =
        LoggerMessage.Define<string, Guid, string, string, int>(LogLevel.Error,
            new EventId(1003, "SyncFailed"),
            "[{CorrelationId}] Sync failed for account {AccountId}: " +
            "errorCode={ErrorCode}, message={SafeMessage}, attempt={RetryCount}");

    private static readonly Action<ILogger, string, Guid, int, double, string, Exception?> _syncRetrying =
        LoggerMessage.Define<string, Guid, int, double, string>(LogLevel.Warning,
            new EventId(1004, "SyncRetrying"),
            "[{CorrelationId}] Sync retrying for account {AccountId}: " +
            "attempt {Attempt}, delay {DelaySeconds}s, reason={Reason}");

    private static readonly Action<ILogger, string, Guid, Exception?> _credentialAccessed =
        LoggerMessage.Define<string, Guid>(LogLevel.Debug,
            new EventId(1005, "CredentialAccessed"),
            "[{CorrelationId}] Credential accessed for account {AccountId}");

    private static readonly Action<ILogger, string, string, string, Exception?> _webhookReceived =
        LoggerMessage.Define<string, string, string>(LogLevel.Information,
            new EventId(1006, "WebhookReceived"),
            "[{CorrelationId}] Plaid webhook received: type={WebhookType}, code={WebhookCode}");

    public BankSyncLogger(ILogger<BankSyncLogger> logger) => _logger = logger;

    public void SyncStarted(string correlationId, Guid accountId)
        => _syncStarted(_logger, correlationId, accountId, null);

    public void SyncCompleted(string correlationId, Guid accountId,
        int transactionsFetched, int deduped, long durationMs)
        => _syncCompleted(_logger, correlationId, accountId, transactionsFetched, deduped, durationMs, null);

    public void SyncFailed(string correlationId, Guid accountId,
        string errorCode, string safeMessage, int retryCount)
        => _syncFailed(_logger, correlationId, accountId, errorCode, safeMessage, retryCount, null);

    public void SyncRetrying(string correlationId, Guid accountId,
        int attempt, TimeSpan delay, string reason)
        => _syncRetrying(_logger, correlationId, accountId, attempt, delay.TotalSeconds, reason, null);

    public void CredentialAccessed(string correlationId, Guid accountId)
        => _credentialAccessed(_logger, correlationId, accountId, null);

    public void WebhookReceived(string correlationId, string webhookType, string webhookCode)
        => _webhookReceived(_logger, correlationId, webhookType, webhookCode, null);
}
