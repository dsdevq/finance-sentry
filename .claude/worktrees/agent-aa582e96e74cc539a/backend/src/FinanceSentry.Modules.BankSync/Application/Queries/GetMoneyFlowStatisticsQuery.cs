namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Core.Cqrs;

// ── Query ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns monthly cash-flow statistics (inflow / outflow / net) for a user.
/// </summary>
public record GetMoneyFlowStatisticsQuery(Guid UserId, int Months = 6) : IQuery<IReadOnlyList<MonthlyFlow>>;

// ── Handler ────────────────────────────────────────────────────────────────────

public class GetMoneyFlowStatisticsQueryHandler(IMoneyFlowStatisticsService service) : IQueryHandler<GetMoneyFlowStatisticsQuery, IReadOnlyList<MonthlyFlow>>
{
    private readonly IMoneyFlowStatisticsService _service = service;

    public async Task<IReadOnlyList<MonthlyFlow>> Handle(
          GetMoneyFlowStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetMonthlyFlowAsync(request.UserId, request.Months, cancellationToken);
    }
}
