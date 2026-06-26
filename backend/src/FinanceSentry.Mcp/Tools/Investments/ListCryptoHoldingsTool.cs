using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.CryptoSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.Investments;

public sealed class ListCryptoHoldingsTool(
    IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse> handler) : IMcpTool
{
    public string Name => "list_crypto_holdings";
    public string Description => "Returns crypto portfolio holdings for the authenticated user including asset symbol, quantity, and current value per holding.";
    public bool IsReadOnly => true;
    public bool IsStub => false;

    public async Task<McpToolResult> InvokeAsync(
        McpToolContext context,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            parameters.TryGetValue("exchange", out var exchangeVal);
            var exchange = exchangeVal as string;

            var result = await handler.Handle(
                new GetCryptoHoldingsQuery(context.UserId),
                cancellationToken);

            var providerMatches = string.IsNullOrWhiteSpace(exchange)
                || result.Provider.Equals(exchange, StringComparison.OrdinalIgnoreCase);

            IEnumerable<CryptoHoldingDto> holdingItems = providerMatches ? result.Holdings : [];

            var payload = new
            {
                provider = result.Provider,
                syncedAt = result.SyncedAt,
                isStale = result.IsStale,
                totalUsdValue = result.TotalUsdValue,
                holdings = holdingItems.Select(h => new
                {
                    symbol = h.Asset,
                    quantity = h.FreeQuantity + h.LockedQuantity,
                    currentValue = h.UsdValue,
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
