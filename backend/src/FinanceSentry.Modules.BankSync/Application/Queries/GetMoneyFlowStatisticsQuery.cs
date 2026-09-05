namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Core.Cqrs;

// ── Query ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns monthly cash-flow statistics (inflow / outflow / net) for a user.
/// </summary>
public record GetMoneyFlowStatisticsQuery(Guid UserId, int Months = 6) : IQuery<IReadOnlyList<MonthlyFlow>>;

// ── Handler ────────────────────────────────────────────────────────────────────

public class GetMoneyFlowStatisticsQueryHandler(
    IMoneyFlowStatisticsService service,
    ICounterpartyClassificationService classification) : IQueryHandler<GetMoneyFlowStatisticsQuery, IReadOnlyList<MonthlyFlow>>
{
    private readonly IMoneyFlowStatisticsService _service = service;
    private readonly ICounterpartyClassificationService _classification = classification;

    public async Task<IReadOnlyList<MonthlyFlow>> Handle(
          GetMoneyFlowStatisticsQuery request, CancellationToken cancellationToken)
    {
        var counterparties = await _classification.ClassifyForWindowAsync(
            request.UserId, request.Months, cancellationToken);

        return await _service.GetMonthlyFlowAsync(
            request.UserId, counterparties, request.Months, cancellationToken);
    }
}
