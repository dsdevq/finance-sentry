namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Failure signal from <see cref="IIBKRConnector"/>. Carries a stable
/// <see cref="ErrorCode"/> the frontend maps to a user-facing message and an
/// HTTP status the controller returns verbatim.
/// </summary>
public sealed class IBKRConnectException(string errorCode, int statusCode, string message)
    : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
    public int StatusCode { get; } = statusCode;
}
