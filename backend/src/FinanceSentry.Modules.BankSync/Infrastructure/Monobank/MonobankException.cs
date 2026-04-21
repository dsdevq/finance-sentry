namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank;

public class MonobankException : Exception
{
    public string ErrorCode { get; }
    public int? HttpStatus { get; }

    public MonobankException(string errorCode, string message, int? httpStatus = null)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatus = httpStatus;
    }
}
