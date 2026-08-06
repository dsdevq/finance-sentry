using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR.OAuth;

/// <summary>
/// <see cref="IBrokerAdapter"/> backed by IBKR OAuth 1.0a. Loads the user's
/// stored artifacts, resolves them to signing material, and reads the portfolio
/// through <see cref="IbkrOAuthClient"/>. The live session token is cached in
/// the client, so the repeated resolves across one sync are cheap.
/// </summary>
public sealed class IbkrOAuthAdapter(
    IIBKRCredentialRepository credentialRepository,
    IIbkrCredentialResolver resolver,
    IbkrOAuthClient client) : IBrokerAdapter
{
    public string BrokerName => "IBKR";

    public async Task EnsureSessionAsync(Guid credentialId, CancellationToken ct = default)
    {
        using var credentials = await ResolveAsync(credentialId, ct);
        var accounts = await client.GetAccountsAsync(credentials, ct);
        if (accounts.Count == 0)
            throw new BrokerAuthException(
                "IBKR OAuth session returned no accounts — the consumer key may not be active yet.", "IBKR");
    }

    public async Task<string> GetAccountIdAsync(Guid credentialId, CancellationToken ct = default)
    {
        using var credentials = await ResolveAsync(credentialId, ct);
        var accounts = await client.GetAccountsAsync(credentials, ct);
        if (accounts.Count == 0)
            throw new InvalidOperationException("No IBKR accounts found for the authenticated session.");
        return accounts[0];
    }

    public async Task<IReadOnlyList<BrokerPosition>> GetPositionsAsync(
        Guid credentialId, string accountId, CancellationToken ct = default)
    {
        using var credentials = await ResolveAsync(credentialId, ct);
        var positions = await client.GetPositionsAsync(credentials, accountId, ct);
        return positions
            .Select(p => new BrokerPosition(
                Symbol: p.ContractDesc,
                InstrumentType: p.AssetClass,
                Quantity: p.Position,
                UsdValue: p.MktValue,
                AverageCostUsd: p.AvgPrice ?? p.AvgCost))
            .ToList();
    }

    private async Task<IbkrOAuthCredentials> ResolveAsync(Guid credentialId, CancellationToken ct)
    {
        var credential = await credentialRepository.GetByIdAsync(credentialId, ct)
            ?? throw new BrokerAuthException(
                $"No IBKR credential found for id {credentialId}.", "IBKR");
        return resolver.Resolve(credential);
    }
}
