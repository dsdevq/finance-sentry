using System.ComponentModel;
using FinanceSentry.Mcp.Abstractions;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools.Stubs;

[McpServerToolType]
public sealed class CashflowReportStub : IReadOnlyMcpTool
{
    public string ToolName => "get_cashflow_report";

    [McpServerTool(Name = "get_cashflow_report")]
    [Description("Returns a cashflow report for a given period. Not yet available — Cashflow reporting module not yet implemented.")]
    public NotYetAvailablePayload Execute() =>
        new("not_yet_available", "Cashflow reporting module not yet implemented");
}
