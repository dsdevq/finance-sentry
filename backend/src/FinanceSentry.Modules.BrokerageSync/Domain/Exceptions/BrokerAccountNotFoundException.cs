namespace FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;

public sealed class BrokerAccountNotFoundException : Exception
{
    public BrokerAccountNotFoundException(string message) : base(message) { }
}
