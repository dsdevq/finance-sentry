namespace FinanceSentry.Core.Interfaces;

public interface IBankingTotalsReader
{
    Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(CancellationToken ct = default);
    Task<decimal> GetTotalUsdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// The most recent <b>successful</b> sync time across the user's active banking accounts, or
    /// null when none have ever synced. Used to decide whether the banking sleeve is fresh enough
    /// to record as live, or stale (a lapsed connection) and better carried forward.
    /// </summary>
    Task<DateTime?> GetLatestSuccessfulSyncAsync(Guid userId, CancellationToken ct = default);
}
