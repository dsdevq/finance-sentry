namespace FinanceSentry.Modules.BankSync.Infrastructure.Services;

/// <summary>
/// Maps Plaid error codes to HTTP status codes and user-safe messages.
/// Prevents Plaid internals from leaking into API responses.
/// </summary>
public interface IPlaidErrorMapper
{
    /// <summary>
    /// Maps a Plaid error code string to a user-facing result.
    /// </summary>
    PlaidErrorResult Map(string errorCode);
}

/// <summary>
/// The result of mapping a Plaid error code.
/// </summary>
public record PlaidErrorResult(int HttpStatusCode, string UserMessage);

/// <inheritdoc />
public class PlaidErrorMapper : IPlaidErrorMapper
{
    /// <inheritdoc />
    public PlaidErrorResult Map(string errorCode) => errorCode switch
    {
        "ITEM_LOGIN_REQUIRED"   => new(401, "Bank credentials expired. Please reconnect your account."),
        "INVALID_CREDENTIALS"   => new(401, "Invalid bank credentials"),
        "RATE_LIMIT_EXCEEDED"   => new(429, "Rate limited. Retrying automatically..."),
        "INVALID_REQUEST"       => new(400, "Invalid request to bank. Please try again."),
        "SERVER_ERROR"          => new(503, "Bank API temporarily unavailable. Retrying..."),
        "INTERNAL_SERVER_ERROR" => new(503, "Bank API temporarily unavailable. Retrying..."),
        "PRODUCT_NOT_READY"     => new(503, "Bank data not ready yet. Will retry automatically."),
        _                       => new(500, "Bank sync error. Please try again.")
    };
}
