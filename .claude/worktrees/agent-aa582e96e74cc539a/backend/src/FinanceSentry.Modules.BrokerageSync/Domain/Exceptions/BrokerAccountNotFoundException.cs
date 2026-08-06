using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;

public sealed class BrokerAccountNotFoundException(string message)
    : ApiException(404, "NOT_CONNECTED", message);
