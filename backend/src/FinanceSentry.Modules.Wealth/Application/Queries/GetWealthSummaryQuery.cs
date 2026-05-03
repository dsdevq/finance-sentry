namespace FinanceSentry.Modules.Wealth.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Wealth.Domain.Services;

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

public record GetWealthSummaryQuery(
    Guid UserId,
    string? Category = null,
    string? Provider = null) : IQuery<WealthSummaryResponse>;

public class GetWealthSummaryQueryHandler(IWealthAggregationService service)
    : IQueryHandler<GetWealthSummaryQuery, WealthSummaryResponse>
{
    private readonly IWealthAggregationService _service = service;

    public Task<WealthSummaryResponse> Handle(GetWealthSummaryQuery request, CancellationToken cancellationToken)
        => _service.GetWealthSummaryAsync(request.UserId, request.Category, request.Provider, cancellationToken);
}
