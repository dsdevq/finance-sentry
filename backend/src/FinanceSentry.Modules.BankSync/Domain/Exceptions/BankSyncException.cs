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
public class PlaidApiException : BankSyncException
{
    public string PlaidErrorCode { get; }

    public PlaidApiException(string plaidErrorCode, string message, int httpStatusCode = 400)
        : base("PLAID_ERROR", message, httpStatusCode)
    {
        PlaidErrorCode = plaidErrorCode;
    }
}

/// <summary>
/// Thrown when a bank account is not found or not accessible by the requesting user.
/// </summary>
public class AccountNotFoundException : BankSyncException
{
    public AccountNotFoundException(Guid accountId)
        : base("ACCOUNT_NOT_FOUND", $"Account {accountId} not found.", 404) { }
}

/// <summary>
/// Thrown when a sync is attempted but one is already running.
/// </summary>
public class SyncAlreadyRunningException : BankSyncException
{
    public SyncAlreadyRunningException(Guid accountId)
        : base("SYNC_ALREADY_RUNNING", "A sync is already in progress for this account.", 409) { }
}

/// <summary>
/// Thrown when credentials have expired and re-authentication is required.
/// </summary>
public class CredentialExpiredException : BankSyncException
{
    public CredentialExpiredException()
        : base("CREDENTIAL_EXPIRED", "Bank credentials expired. Please reconnect your account.", 401) { }
}
