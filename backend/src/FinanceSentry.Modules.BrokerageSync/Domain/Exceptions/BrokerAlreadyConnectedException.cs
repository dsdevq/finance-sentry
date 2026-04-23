namespace FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;

public sealed class BrokerAlreadyConnectedException : Exception
{
    public BrokerAlreadyConnectedException(string message) : base(message) { }
}
