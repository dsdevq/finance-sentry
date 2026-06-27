using System.ComponentModel;
using System.Text.Json.Serialization;
using FinanceSentry.Mcp.Abstractions;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools.Stubs;

[McpServerToolType]
public sealed class CryptoPnlDetailStub : IReadOnlyMcpTool
{
    public string ToolName => "get_crypto_pnl_detail";

    [McpServerTool(Name = "get_crypto_pnl_detail")]
    [Description("Returns crypto P&L breakdown per asset. Not yet available — Crypto P&L calculation has not been implemented in the CryptoSync module.")]
    public NotYetAvailablePayload Execute() =>
        new("not_yet_available", "Crypto P&L calculation not yet implemented in CryptoSync module");
}

public sealed record NotYetAvailablePayload(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reason")] string Reason);
