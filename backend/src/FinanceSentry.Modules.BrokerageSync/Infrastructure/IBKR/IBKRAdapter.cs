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
        // Read-only IBKR users only ever hold the tier-1 Portal session — they
        // cannot open a tier-2 /iserver brokerage session (IBKR requires trading
        // permissions for that). Portfolio reads need only tier 1, so a live
        // session means /portfolio/accounts returns at least one account.
        var baseUrl = _resolver.BaseUrl(credentialId);
        try
        {
            var accounts = await _client.GetAccountsAsync(baseUrl, ct);
            if (accounts.Accounts.Count == 0)
                throw new BrokerAuthException(
                    "IBKR gateway has no active Portal session for this user. The IBeam "
                        + "container may still be starting, or login/2FA has not completed.",
                    "IBKR");
        }
        catch (HttpRequestException ex)
        {
            throw new BrokerAuthException(
                "IBKR gateway is unreachable for this user. The IBeam container may still be starting.",
                "IBKR", ex);
        }
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
