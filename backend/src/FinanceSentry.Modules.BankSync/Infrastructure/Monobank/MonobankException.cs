using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank;

public class MonobankException(string errorCode, string message, int httpStatus = 500)
    : ApiException(httpStatus, errorCode, message);
