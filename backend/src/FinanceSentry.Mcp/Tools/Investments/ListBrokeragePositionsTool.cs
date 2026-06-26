using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.Investments;

public sealed class ListBrokeragePositionsTool(
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> holdings) : IMcpTool
{
    public string Name => "list_brokerage_positions";
    public string Description => "Returns brokerage portfolio positions for the authenticated user including symbol, quantity, and current value per position.";
    public bool IsReadOnly => true;
    public bool IsStub => false;
    public string? StubReason => null;

    public async Task<McpToolResult> InvokeAsync(
        Guid userId,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await holdings.Handle(new GetBrokerageHoldingsQuery(userId), cancellationToken);
            var payload = result.Positions.Select(p => new
            {
                symbol = p.Symbol,
                quantity = p.Quantity,
                currentValue = p.UsdValue,
            });
            return McpToolResult.Success(payload);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
