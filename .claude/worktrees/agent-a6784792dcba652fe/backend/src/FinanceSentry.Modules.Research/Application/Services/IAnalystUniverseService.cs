namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IAnalystUniverseService
{
    /// <summary>
    /// Recompose the ingestion universe from seed ∪ holdings ∪ watchlist ∪ open candidates, persist
    /// membership, de-activate departed members, and return the active set (feature 030, FR-002).
    /// </summary>
    Task<IReadOnlyList<AnalystUniverseMember>> SyncAsync(CancellationToken ct = default);
}
