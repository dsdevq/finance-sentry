namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>
/// Reads the cached "Ledger's read" for a ticker (feature 421, US3). Never invokes the agent —
/// this is the instant path the dossier page renders on load.
/// </summary>
public sealed record GetAssetLedgerReadQuery(Guid UserId, string Symbol) : IQuery<AssetLedgerReadResult>;

public sealed class GetAssetLedgerReadQueryHandler(
    IAssetLedgerReadRepository repository,
    IQueryHandler<GetAssetDossierQuery, AssetDossierResult> dossier)
    : IQueryHandler<GetAssetLedgerReadQuery, AssetLedgerReadResult>
{
    public async Task<AssetLedgerReadResult> Handle(GetAssetLedgerReadQuery request, CancellationToken ct)
    {
        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var cached = await repository.GetAsync(request.UserId, symbol, ct);

        if (cached is null)
        {
            return new AssetLedgerReadResult(symbol, null, null, IsStale: true, Cached: false);
        }

        // Data-change invalidation needs the current dossier fingerprint. The fan-out is the same
        // one the dossier endpoint runs and is individually fault-tolerant; if it fails outright we
        // still serve the cached narrative and fall back to age-only staleness.
        string? currentFingerprint = null;
        try
        {
            currentFingerprint = LedgerReadComposer.Fingerprint(
                await dossier.Handle(new GetAssetDossierQuery(request.UserId, symbol), ct));
        }
        catch
        {
            // degrade to age-only staleness
        }

        return new AssetLedgerReadResult(
            symbol,
            cached.Narrative,
            cached.GeneratedAt,
            IsStale: LedgerReadStaleness.IsStale(cached, currentFingerprint),
            Cached: true);
    }
}
