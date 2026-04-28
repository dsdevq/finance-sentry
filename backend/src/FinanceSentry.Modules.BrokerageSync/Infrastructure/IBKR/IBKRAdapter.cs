using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed class IBKRAdapter : IBrokerAdapter
{
    private readonly IBKRGatewayClient _client;

    public IBKRAdapter(IBKRGatewayClient client)
    {
        _client = client;
    }

    public string BrokerName => "IBKR";

    public async Task EnsureSessionAsync(CancellationToken ct = default)
    {
        var status = await _client.GetAuthStatusAsync(ct);
        if (!status.Authenticated)
            throw new BrokerAuthException(
                "IBKR gateway is not authenticated. Configure IBKR_ACCOUNT/IBKR_PASSWORD in docker/.env and ensure the ibkr-gateway container is running.",
                "IBKR");
    }

    public async Task<string> GetAccountIdAsync(CancellationToken ct = default)
    {
        var accountsResponse = await _client.GetAccountsAsync(ct);
        if (accountsResponse.Accounts.Count == 0)
            throw new InvalidOperationException("No IBKR accounts found for the authenticated session.");

        return accountsResponse.Accounts[0];
    }

    public async Task<IReadOnlyList<BrokerPosition>> GetPositionsAsync(string accountId, CancellationToken ct = default)
    {
        var positions = await _client.GetPositionsAsync(accountId, ct);
        return positions
            .Select(p => new BrokerPosition(
                Symbol: p.ContractDesc,
                InstrumentType: p.AssetClass,
                Quantity: p.Position,
                UsdValue: p.MktValue))
            .ToList();
    }
}
