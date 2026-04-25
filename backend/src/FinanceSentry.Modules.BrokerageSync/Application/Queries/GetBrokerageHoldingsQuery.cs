using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Queries;

public sealed record GetBrokerageHoldingsQuery(Guid UserId) : IQuery<BrokerageHoldingsResponse>;

public sealed record BrokeragePositionDto(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal UsdValue);

public sealed record BrokerageHoldingsResponse(
    string Provider,
    DateTime? SyncedAt,
    bool IsStale,
    IReadOnlyList<BrokeragePositionDto> Positions,
    decimal TotalUsdValue);

public sealed class GetBrokerageHoldingsQueryHandler
    : IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse>
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(1);

    private readonly IBrokerageHoldingRepository _holdingRepository;

    public GetBrokerageHoldingsQueryHandler(IBrokerageHoldingRepository holdingRepository)
    {
        _holdingRepository = holdingRepository;
    }

    public async Task<BrokerageHoldingsResponse> Handle(
        GetBrokerageHoldingsQuery request, CancellationToken ct)
    {
        var holdings = await _holdingRepository.GetByUserIdAsync(request.UserId, ct);

        if (holdings.Count == 0)
        {
            return new BrokerageHoldingsResponse(
                Provider: "ibkr",
                SyncedAt: null,
                IsStale: false,
                Positions: [],
                TotalUsdValue: 0m);
        }

        var latestSyncedAt = holdings.Max(h => h.SyncedAt);
        var isStale = DateTime.UtcNow - latestSyncedAt > StaleThreshold;
        var totalUsd = holdings.Sum(h => h.UsdValue);

        var positions = holdings
            .Select(h => new BrokeragePositionDto(h.Symbol, h.InstrumentType, h.Quantity, h.UsdValue))
            .ToList();

        return new BrokerageHoldingsResponse(
            Provider: "ibkr",
            SyncedAt: latestSyncedAt,
            IsStale: isStale,
            Positions: positions,
            TotalUsdValue: totalUsd);
    }
}
