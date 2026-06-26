using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.Investments;

public sealed class ListBrokeragePositionsTool(
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> handler) : IMcpTool
{
    public string Name => "list_brokerage_positions";
    public string Description => "Returns brokerage portfolio positions for the authenticated user including symbol, quantity, and current value per position.";
    public bool IsReadOnly => true;
    public bool IsStub => false;

    public async Task<McpToolResult> InvokeAsync(
        McpToolContext context,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.Handle(
                new GetBrokerageHoldingsQuery(context.UserId),
                cancellationToken);

            var payload = new
            {
                provider = result.Provider,
                syncedAt = result.SyncedAt,
                isStale = result.IsStale,
                totalUsdValue = result.TotalUsdValue,
                positions = result.Positions.Select(p => new
                {
                    symbol = p.Symbol,
                    quantity = p.Quantity,
                    currentValue = p.UsdValue,
                }).ToList(),
            };

            return McpToolResult.Success(payload);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
