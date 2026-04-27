namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Domain.Services;
using FinanceSentry.Core.Cqrs;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record AppliedFiltersDto(string? Category, string? Provider);

public record AccountBalanceDto(
    Guid AccountId,
    string BankName,
    string AccountType,
    string AccountNumberLast4,
    string Provider,
    string Category,
    string Currency,
    decimal? CurrentBalance,
    decimal? BalanceInBaseCurrency,
    string SyncStatus,
    DateTime? LastSyncTimestamp);

public record CategorySummaryDto(
    string Category,
    decimal TotalInBaseCurrency,
    IReadOnlyList<AccountBalanceDto> Accounts);

public record WealthSummaryResponse(
    decimal TotalNetWorth,
    string BaseCurrency,
    IReadOnlyList<CategorySummaryDto> Categories,
    AppliedFiltersDto AppliedFilters);

// ── Query ────────────────────────────────────────────────────────────────────

public record GetWealthSummaryQuery(
    Guid UserId,
    string? Category = null,
    string? Provider = null
) : IQuery<WealthSummaryResponse>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetWealthSummaryQueryHandler(IWealthAggregationService service)
    : IQueryHandler<GetWealthSummaryQuery, WealthSummaryResponse>
{
    private readonly IWealthAggregationService _service = service;

    public Task<WealthSummaryResponse> Handle(GetWealthSummaryQuery request, CancellationToken cancellationToken)
        => _service.GetWealthSummaryAsync(request.UserId, request.Category, request.Provider, cancellationToken);
}
