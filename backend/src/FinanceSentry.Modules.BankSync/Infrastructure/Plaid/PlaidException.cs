namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

/// <summary>
/// Exception thrown when the Plaid API returns an error response.
/// IsTransient indicates whether the error is retryable (FR-005).
/// Permanent errors (4xx) must NOT be retried — only transient errors (429, 5xx).
/// </summary>
public class PlaidException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    /// <summary>True for 429 and 5xx — retryable with exponential backoff (FR-005).</summary>
    public bool IsTransient => StatusCode == 429 || StatusCode >= 500;

    public PlaidException(int statusCode, string errorCode, string message)
        : base($"Plaid API error {statusCode} ({errorCode}): {message}")
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
