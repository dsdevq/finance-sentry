namespace FinanceSentry.Modules.Wealth.Domain.Services;

using FinanceSentry.Modules.Wealth.Application.Queries;

public interface IWealthAggregationService
{
    Task<WealthSummaryResponse> GetWealthSummaryAsync(
        Guid userId, string? category, string? provider, CancellationToken ct = default);

    Task<TransactionSummaryResponse> GetTransactionSummaryAsync(
        Guid userId, DateOnly from, DateOnly to,
        string? category, string? provider, CancellationToken ct = default);
}
