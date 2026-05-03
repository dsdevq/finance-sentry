namespace FinanceSentry.Core.Interfaces;

public record ProviderMonthlyBalance(
    DateOnly MonthEnd,
    decimal TotalUsd,
    string AssetCategory);

public interface IProviderMonthlyHistorySource
{
    Task<IReadOnlyList<ProviderMonthlyBalance>> GetMonthlyBalancesAsync(
        Guid userId, CancellationToken ct = default);
}
