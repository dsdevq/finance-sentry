using FinanceSentry.Core.Exceptions;

namespace FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;

public sealed class BrokerAuthException : ApiException
{
    public string? BrokerName { get; }

    public BrokerAuthException(string message, string? brokerName = null)
        : base(422, "INVALID_CREDENTIALS", message)
    {
        BrokerName = brokerName;
    }

    public BrokerAuthException(string message, string? brokerName, Exception inner)
        : base(422, "INVALID_CREDENTIALS", message, inner)
    {
        BrokerName = brokerName;
    }
}
