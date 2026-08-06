using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.CryptoSync.Domain.Exceptions;

/// <summary>
/// Base for Binance API and credential failures. Defaults to 422 / INVALID_CREDENTIALS
/// (the status the previous controller catch produced for any BinanceException without
/// a more specific code). Typed subclasses below claim other HTTP statuses.
/// </summary>
public class BinanceException : ApiException
{
    public int? BinanceErrorCode { get; }

    public BinanceException(string message, int? binanceErrorCode = null)
        : base(422, "INVALID_CREDENTIALS", message)
    {
        BinanceErrorCode = binanceErrorCode;
    }

    public BinanceException(string message, Exception innerException, int? binanceErrorCode = null)
        : base(422, "INVALID_CREDENTIALS", message, innerException)
    {
        BinanceErrorCode = binanceErrorCode;
    }
}

public sealed class BinanceAlreadyConnectedException()
    : ApiException(409, "ALREADY_CONNECTED",
        "A Binance account is already connected for this user.");

public sealed class BinanceAccountNotFoundException()
    : ApiException(404, "NOT_FOUND",
        "No Binance account is connected for this user.");
