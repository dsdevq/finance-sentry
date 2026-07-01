using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed class IBKRAdapter : IBrokerAdapter
{
    private readonly IBKRGatewayClient _client;
    private readonly IIBeamGatewayResolver _resolver;

    public IBKRAdapter(IBKRGatewayClient client, IIBeamGatewayResolver resolver)
    {
        _client = client;
        _resolver = resolver;
    }

    public string BrokerName => "IBKR";

    public async Task EnsureSessionAsync(Guid credentialId, CancellationToken ct = default)
    {
        var baseUrl = _resolver.BaseUrl(credentialId);
        var status = await _client.GetAuthStatusAsync(baseUrl, ct);
        if (!status.Authenticated)
            throw new BrokerAuthException(
                "IBKR gateway is not authenticated for this user. The IBeam container may still be starting or the stored credentials may be invalid.",
                "IBKR");
    }

    public async Task<string> GetAccountIdAsync(Guid credentialId, CancellationToken ct = default)
    {
        var baseUrl = _resolver.BaseUrl(credentialId);
        var accountsResponse = await _client.GetAccountsAsync(baseUrl, ct);
        if (accountsResponse.Accounts.Count == 0)
            throw new InvalidOperationException("No IBKR accounts found for the authenticated session.");

        return accountsResponse.Accounts[0];
    }

    public async Task<IReadOnlyList<BrokerPosition>> GetPositionsAsync(Guid credentialId, string accountId, CancellationToken ct = default)
    {
        var baseUrl = _resolver.BaseUrl(credentialId);
        var positions = await _client.GetPositionsAsync(baseUrl, accountId, ct);
        return positions
            .Select(p => new BrokerPosition(
                Symbol: p.ContractDesc,
                InstrumentType: p.AssetClass,
                Quantity: p.Position,
                UsdValue: p.MktValue,
                AverageCostUsd: p.AvgPrice ?? p.AvgCost))
            .ToList();
    }
}
