using System.ComponentModel;
using FinanceSentry.Mcp.Abstractions;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools.Stubs;

[McpServerToolType]
public sealed class NetWorthHistoryStub : IReadOnlyMcpTool
{
    public string ToolName => "get_net_worth_history";

    [McpServerTool(Name = "get_net_worth_history")]
    [Description("Returns historical net worth snapshots over time. Not yet available — Historical net worth snapshots not yet implemented.")]
    public NotYetAvailablePayload Execute() =>
        new("not_yet_available", "Historical net worth snapshots not yet implemented");
}
