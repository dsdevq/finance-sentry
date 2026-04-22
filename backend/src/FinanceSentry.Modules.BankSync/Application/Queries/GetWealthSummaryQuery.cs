namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Domain.Services;
using MediatR;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record AppliedFiltersDto(string? Category, string? Provider);

public record AccountBalanceDto(
    Guid Id,
    string BankName,
    string AccountType,
    string AccountNumberLast4,
    string Provider,
    string Category,
    string Currency,
    decimal? NativeBalance,
    decimal? BalanceInBaseCurrency,
    string SyncStatus);

public record CategorySummaryDto(
    string Name,
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
) : IRequest<WealthSummaryResponse>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetWealthSummaryQueryHandler(IWealthAggregationService service)
    : IRequestHandler<GetWealthSummaryQuery, WealthSummaryResponse>
{
    private readonly IWealthAggregationService _service = service;

    public Task<WealthSummaryResponse> Handle(GetWealthSummaryQuery request, CancellationToken cancellationToken)
        => _service.GetWealthSummaryAsync(request.UserId, request.Category, request.Provider, cancellationToken);
}
