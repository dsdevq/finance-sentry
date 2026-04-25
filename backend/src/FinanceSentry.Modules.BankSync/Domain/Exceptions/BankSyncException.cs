namespace FinanceSentry.Modules.BankSync.Domain.Exceptions;

/// <summary>
/// Base exception for all bank sync domain errors.
/// </summary>
public class BankSyncException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    public BankSyncException(string errorCode, string message, int httpStatusCode = 500)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    public BankSyncException(string errorCode, string message, Exception inner, int httpStatusCode = 500)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}

/// <summary>
/// Thrown when Plaid returns an error that maps to a user-facing message.
/// </summary>
public class PlaidApiException(string plaidErrorCode, string message, int httpStatusCode = 400) : BankSyncException("PLAID_ERROR", message, httpStatusCode)
{
    public string PlaidErrorCode { get; } = plaidErrorCode;
}

/// <summary>
/// Thrown when a bank account is not found or not accessible by the requesting user.
/// </summary>
public class AccountNotFoundException(Guid accountId) : BankSyncException("ACCOUNT_NOT_FOUND", $"Account {accountId} not found.", 404)
{
}

/// <summary>
/// Thrown when a sync is attempted but one is already running.
/// </summary>
public class SyncAlreadyRunningException() : BankSyncException("SYNC_ALREADY_RUNNING", "A sync is already in progress for this account.", 409)
{
}

/// <summary>
/// Thrown when credentials have expired and re-authentication is required.
/// </summary>
public class CredentialExpiredException : BankSyncException
{
    public CredentialExpiredException()
        : base("CREDENTIAL_EXPIRED", "Bank credentials expired. Please reconnect your account.", 401) { }
}
