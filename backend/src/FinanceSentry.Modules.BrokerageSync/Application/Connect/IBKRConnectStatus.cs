using System.Text.Json.Serialization;

namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// State machine for an async IBKR connect session. The frontend polls
/// GET /brokerage/ibkr/connect/{sessionId} until it observes a terminal state
/// (Completed | Failed | Cancelled). Serialized as camelCase strings on the
/// wire so the frontend can string-match instead of tracking int values.
/// </summary>
[JsonConverter(typeof(CamelCaseStatusConverter))]
public enum IBKRConnectStatus
{
    Pending,
    Spawning,
    AwaitingAuth,
    Syncing,
    Completed,
    Failed,
    Cancelled,
}

internal sealed class CamelCaseStatusConverter : JsonStringEnumConverter<IBKRConnectStatus>
{
    public CamelCaseStatusConverter()
        : base(namingPolicy: System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
