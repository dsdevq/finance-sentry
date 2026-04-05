namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Application.Services;
using MediatR;

// ── Query ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the aggregated current balance per currency and total account count for a user.
/// </summary>
public record GetAggregatedBalanceQuery(Guid UserId) : IRequest<AggregatedBalanceResult>;

// ── Result ─────────────────────────────────────────────────────────────────────

public record AggregatedBalanceResult(
    Dictionary<string, decimal> Balances,
    int TotalAccountCount);

// ── Handler ────────────────────────────────────────────────────────────────────

public class GetAggregatedBalanceQueryHandler(IAggregationService aggregation) : IRequestHandler<GetAggregatedBalanceQuery, AggregatedBalanceResult>
{
    private readonly IAggregationService _aggregation = aggregation;

    public async Task<AggregatedBalanceResult> Handle(
          GetAggregatedBalanceQuery request, CancellationToken cancellationToken)
    {
        var balances = await _aggregation.GetAggregatedBalanceAsync(request.UserId, cancellationToken);
        var byType = await _aggregation.GetAccountCountByTypeAsync(request.UserId, cancellationToken);
        var totalCount = byType.Values.Sum();

        return new AggregatedBalanceResult(balances, totalCount);
    }
}
