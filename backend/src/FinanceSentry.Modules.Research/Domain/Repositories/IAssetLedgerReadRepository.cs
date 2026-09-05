namespace FinanceSentry.Modules.Research.Domain.Repositories;

using FinanceSentry.Modules.Research.Domain;

public interface IAssetLedgerReadRepository
{
    Task<AssetLedgerRead?> GetAsync(Guid userId, string symbol, CancellationToken ct = default);

    /// <summary>Inserts or overwrites the single cached read for (user, symbol).</summary>
    Task UpsertAsync(AssetLedgerRead read, CancellationToken ct = default);
}
