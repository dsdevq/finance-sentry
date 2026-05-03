namespace FinanceSentry.Core.Interfaces;

public interface IBankingTotalsReader
{
    Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(CancellationToken ct = default);
    Task<decimal> GetTotalUsdAsync(Guid userId, CancellationToken ct = default);
}
