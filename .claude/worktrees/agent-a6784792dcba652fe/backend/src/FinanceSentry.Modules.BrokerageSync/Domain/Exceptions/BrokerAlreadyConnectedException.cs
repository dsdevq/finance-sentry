using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;

public sealed class BrokerAlreadyConnectedException(string message)
    : ApiException(409, "ALREADY_CONNECTED", message);
