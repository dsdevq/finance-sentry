using System.ComponentModel;
using FinanceSentry.Mcp.Abstractions;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools.Stubs;

[McpServerToolType]
public sealed class TaxLotsStub : IReadOnlyMcpTool
{
    public string ToolName => "get_tax_lots";

    [McpServerTool(Name = "get_tax_lots")]
    [Description("Returns tax lot details per holding. Not yet available — Tax lot tracking has not been implemented in the BrokerageSync module.")]
    public NotYetAvailablePayload Execute() =>
        new("not_yet_available", "Tax lot tracking not yet implemented in BrokerageSync module");
}
