using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;

namespace FinanceSentry.Modules.CryptoSync.Application.Queries;

public sealed record GetCryptoHoldingsQuery(Guid UserId) : IQuery<CryptoHoldingsResponse>;

public sealed record CryptoHoldingsResponse(
    string Provider,
    DateTime? SyncedAt,
    bool IsStale,
    IReadOnlyList<CryptoHoldingDto> Holdings,
    decimal TotalUsdValue);

public sealed record CryptoHoldingDto(
    string Asset,
    decimal FreeQuantity,
    decimal LockedQuantity,
    decimal UsdValue);

public sealed class GetCryptoHoldingsQueryHandler : IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse>
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(1);

    private readonly ICryptoHoldingRepository _holdingRepository;

    public GetCryptoHoldingsQueryHandler(ICryptoHoldingRepository holdingRepository)
    {
        _holdingRepository = holdingRepository;
    }

    public async Task<CryptoHoldingsResponse> Handle(GetCryptoHoldingsQuery request, CancellationToken ct)
    {
        var holdings = await _holdingRepository.GetByUserIdAsync(request.UserId, ct);

        if (holdings.Count == 0)
        {
            return new CryptoHoldingsResponse(
                Provider: "binance",
                SyncedAt: null,
                IsStale: false,
                Holdings: [],
                TotalUsdValue: 0m);
        }

        var lastSyncedAt = holdings.Max(h => h.SyncedAt);
        var isStale = DateTime.UtcNow - lastSyncedAt > StaleThreshold;

        var dtos = holdings
            .Select(h => new CryptoHoldingDto(h.Asset, h.FreeQuantity, h.LockedQuantity, h.UsdValue))
            .ToList();

        return new CryptoHoldingsResponse(
            Provider: "binance",
            SyncedAt: lastSyncedAt,
            IsStale: isStale,
            Holdings: dtos,
            TotalUsdValue: holdings.Sum(h => h.UsdValue));
    }
}
