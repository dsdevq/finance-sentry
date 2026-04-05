namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Application.Services;
using MediatR;

// ── Query ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the top spending categories for a user sorted by total spend DESC.
/// </summary>
public record GetTopCategoriesQuery(Guid UserId, int Limit = 10) : IRequest<IReadOnlyList<CategoryStat>>;

// ── Handler ────────────────────────────────────────────────────────────────────

public class GetTopCategoriesQueryHandler(IMerchantCategoryStatisticsService service) : IRequestHandler<GetTopCategoriesQuery, IReadOnlyList<CategoryStat>>
{
    private readonly IMerchantCategoryStatisticsService _service = service;

    public async Task<IReadOnlyList<CategoryStat>> Handle(
          GetTopCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetTopCategoriesAsync(request.UserId, request.Limit, cancellationToken);
    }
}
