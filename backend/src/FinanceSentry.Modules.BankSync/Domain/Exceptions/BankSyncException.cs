using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.BankSync.Domain.Exceptions;

/// <summary>
/// Base exception for all bank sync domain errors.
/// </summary>
public class BankSyncException : ApiException
{
    public int HttpStatusCode => StatusCode;

    public BankSyncException(string errorCode, string message, int httpStatusCode = 500)
        : base(httpStatusCode, errorCode, message)
    {
    }

    public BankSyncException(string errorCode, string message, Exception inner, int httpStatusCode = 500)
        : base(httpStatusCode, errorCode, message, inner)
    {
    }
}

public class PlaidApiException(string plaidErrorCode, string message, int httpStatusCode = 400)
    : BankSyncException("PLAID_ERROR", message, httpStatusCode)
{
    public string PlaidErrorCode { get; } = plaidErrorCode;
}

public class AccountNotFoundException(Guid accountId)
    : BankSyncException("ACCOUNT_NOT_FOUND", $"Account {accountId} not found.", 404);

public class SyncAlreadyRunningException()
    : BankSyncException("SYNC_ALREADY_RUNNING", "A sync is already in progress for this account.", 409);

public class CredentialExpiredException()
    : BankSyncException("CREDENTIAL_EXPIRED", "Bank credentials expired. Please reconnect your account.", 401);
