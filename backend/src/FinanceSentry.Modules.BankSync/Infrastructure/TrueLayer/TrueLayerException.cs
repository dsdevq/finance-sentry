namespace FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;

using FinanceSentry.Core.Exceptions;

public class TrueLayerException(string errorCode, string message, int httpStatus = 500)
    : ApiException(httpStatus, errorCode, message);
