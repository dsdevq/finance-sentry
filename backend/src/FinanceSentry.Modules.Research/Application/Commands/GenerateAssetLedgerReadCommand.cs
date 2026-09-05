namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Exceptions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Ports;
using FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>
/// Generates the "Ledger's read" for a ticker through the 040 agent loop and caches it
/// (feature 421, US3). A fresh cached copy short-circuits the agent unless <paramref name="Force"/>.
/// </summary>
public sealed record GenerateAssetLedgerReadCommand(Guid UserId, string Symbol, bool Force)
    : ICommand<AssetLedgerReadResult>;

public sealed class GenerateAssetLedgerReadCommandHandler(
    IQueryHandler<GetAssetDossierQuery, AssetDossierResult> dossierHandler,
    ILedgerNarrator narrator,
    IAssetLedgerReadRepository repository)
    : ICommandHandler<GenerateAssetLedgerReadCommand, AssetLedgerReadResult>
{
    public async Task<AssetLedgerReadResult> Handle(GenerateAssetLedgerReadCommand command, CancellationToken ct)
    {
        var symbol = command.Symbol.Trim().ToUpperInvariant();

        var dossier = await dossierHandler.Handle(new GetAssetDossierQuery(command.UserId, symbol), ct);
        var fingerprint = LedgerReadComposer.Fingerprint(dossier);

        var cached = await repository.GetAsync(command.UserId, symbol, ct);
        if (!command.Force && cached is not null && !LedgerReadStaleness.IsStale(cached, fingerprint))
        {
            return new AssetLedgerReadResult(symbol, cached.Narrative, cached.GeneratedAt, IsStale: false, Cached: true);
        }

        var narrative = await narrator.NarrateAsync(LedgerReadComposer.Prompt(dossier), ct);
        if (string.IsNullOrWhiteSpace(narrative))
        {
            throw new LedgerReadUnavailableException();
        }

        var read = new AssetLedgerRead
        {
            UserId = command.UserId,
            Symbol = symbol,
            Narrative = narrative.Trim(),
            SourceFingerprint = fingerprint,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
        await repository.UpsertAsync(read, ct);

        return new AssetLedgerReadResult(symbol, read.Narrative, read.GeneratedAt, IsStale: false, Cached: false);
    }
}

/// <summary>The agent produced no usable narrative — surfaced as 503 so the UI can offer a retry.</summary>
public sealed class LedgerReadUnavailableException()
    : ApiException(503, "LEDGER_READ_UNAVAILABLE", "Ledger could not produce a read right now. Try again shortly.");
