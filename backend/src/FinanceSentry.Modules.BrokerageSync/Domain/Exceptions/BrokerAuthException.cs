namespace FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;

public sealed class BrokerAuthException : Exception
{
    public string? BrokerName { get; }

    public BrokerAuthException(string message, string? brokerName = null)
        : base(message)
    {
        BrokerName = brokerName;
    }

    public BrokerAuthException(string message, string? brokerName, Exception inner)
        : base(message, inner)
    {
        BrokerName = brokerName;
    }
}
