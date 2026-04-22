namespace FinanceSentry.Modules.CryptoSync.Domain.Exceptions;

public sealed class BinanceException : Exception
{
    public int? BinanceErrorCode { get; }

    public BinanceException(string message, int? binanceErrorCode = null)
        : base(message)
    {
        BinanceErrorCode = binanceErrorCode;
    }

    public BinanceException(string message, Exception innerException, int? binanceErrorCode = null)
        : base(message, innerException)
    {
        BinanceErrorCode = binanceErrorCode;
    }
}
