using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Services;

public sealed class BrokerageHoldingsReader : IBrokerageHoldingsReader
{
    private readonly IBrokerageHoldingRepository _repository;

    public BrokerageHoldingsReader(IBrokerageHoldingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BrokerageHoldingSummary>> GetHoldingsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var holdings = await _repository.GetByUserIdAsync(userId, ct);

        return holdings
            .Select(h => new BrokerageHoldingSummary(
                h.Symbol,
                h.InstrumentType,
                h.Quantity,
                h.UsdValue,
                h.SyncedAt,
                h.Provider))
            .ToList();
    }
}
