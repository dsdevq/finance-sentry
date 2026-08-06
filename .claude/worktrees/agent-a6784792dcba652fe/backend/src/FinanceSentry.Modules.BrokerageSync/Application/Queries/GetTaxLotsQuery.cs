using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Queries;

public sealed record GetTaxLotsQuery(Guid UserId) : IQuery<TaxLotsResponse>;

public sealed record TaxLotsResponse(
    string Provider,
    DateTime? SyncedAt,
    IReadOnlyList<TaxLotDto> Items,
    decimal TotalCostBasisUsd,
    decimal TotalUnrealizedPnlUsd);

public sealed record TaxLotDto(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal CurrentValueUsd,
    decimal? AverageCostUsd,
    decimal? CostBasisUsd,
    decimal? UnrealizedPnlUsd,
    decimal? UnrealizedPnlPercent,
    DateTime? AcquiredAt,
    bool IsLongTerm);

public sealed class GetTaxLotsQueryHandler(IBrokerageHoldingRepository holdingRepository)
    : IQueryHandler<GetTaxLotsQuery, TaxLotsResponse>
{
    private static readonly TimeSpan LongTermThreshold = TimeSpan.FromDays(365);

    private readonly IBrokerageHoldingRepository _holdingRepository = holdingRepository;

    public async Task<TaxLotsResponse> Handle(GetTaxLotsQuery request, CancellationToken ct)
    {
        var holdings = await _holdingRepository.GetByUserIdAsync(request.UserId, ct);

        if (holdings.Count == 0)
            return new TaxLotsResponse("ibkr", null, [], 0m, 0m);

        var now = DateTime.UtcNow;

        var items = holdings
            .Where(h => h.Quantity > 0m)
            .OrderByDescending(h => h.UsdValue)
            .Select(h =>
            {
                decimal? unrealized = h.CostBasisUsd is decimal cb ? h.UsdValue - cb : null;
                decimal? unrealizedPct = h.CostBasisUsd is decimal cb2 && cb2 > 0m
                    ? Math.Round((h.UsdValue - cb2) / cb2 * 100m, 2)
                    : null;
                var isLongTerm = h.AcquiredAt is DateTime acq && (now - acq) >= LongTermThreshold;

                return new TaxLotDto(
                    Symbol: h.Symbol,
                    InstrumentType: h.InstrumentType,
                    Quantity: h.Quantity,
                    CurrentValueUsd: h.UsdValue,
                    AverageCostUsd: h.AverageCostUsd,
                    CostBasisUsd: h.CostBasisUsd,
                    UnrealizedPnlUsd: unrealized,
                    UnrealizedPnlPercent: unrealizedPct,
                    AcquiredAt: h.AcquiredAt,
                    IsLongTerm: isLongTerm);
            })
            .ToList();

        return new TaxLotsResponse(
            Provider: "ibkr",
            SyncedAt: holdings.Max(h => h.SyncedAt),
            Items: items,
            TotalCostBasisUsd: items.Sum(i => i.CostBasisUsd ?? 0m),
            TotalUnrealizedPnlUsd: items.Sum(i => i.UnrealizedPnlUsd ?? 0m));
    }
}
