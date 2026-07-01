namespace FinanceSentry.Modules.Wealth.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Wealth.Domain.Services;

public record AppliedFiltersDto(string? Category, string? Provider);

/// <summary>
/// A single sub-account under an institution: a Monobank card, a Revolut
/// currency pocket, a Binance asset, an IBKR position.
/// </summary>
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

/// <summary>
/// An institution the user has connected: one Monobank customer, one
/// TrueLayer connection (Revolut, AIB), one Binance account, one IBKR account.
/// Every disconnect flow operates at this level.
/// </summary>
public record InstitutionDto(
    Guid InstitutionId,
    string Provider,
    string Name,
    decimal TotalInBaseCurrency,
    string SyncStatus,
    DateTime? LastSyncTimestamp,
    IReadOnlyList<AccountBalanceDto> Accounts);

public record CategorySummaryDto(
    string Category,
    decimal TotalInBaseCurrency,
    int InstitutionCount,
    IReadOnlyList<InstitutionDto> Institutions);

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
