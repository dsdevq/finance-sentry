namespace FinanceSentry.Modules.Wealth.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Wealth.Domain.Services;

public record TransactionCategoryDto(
    string Category,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal NetFlow,
    int TransactionCount);

public record TransactionSummaryResponse(
    string From,
    string To,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal NetFlow,
    IReadOnlyList<TransactionCategoryDto> Categories,
    AppliedFiltersDto AppliedFilters);

public record GetTransactionSummaryQuery(
    Guid UserId,
    DateOnly From,
    DateOnly To,
    string? Category = null,
    string? Provider = null) : IQuery<TransactionSummaryResponse>;

public class GetTransactionSummaryQueryHandler(IWealthAggregationService service)
    : IQueryHandler<GetTransactionSummaryQuery, TransactionSummaryResponse>
{
    private readonly IWealthAggregationService _service = service;

    public Task<TransactionSummaryResponse> Handle(GetTransactionSummaryQuery request, CancellationToken cancellationToken)
        => _service.GetTransactionSummaryAsync(
            request.UserId, request.From, request.To,
            request.Category, request.Provider,
            cancellationToken);
}
