namespace FinanceSentry.Modules.Wealth.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Wealth.Domain;
using FinanceSentry.Modules.Wealth.Domain.Repositories;

public class NetWorthSnapshotService(INetWorthSnapshotRepository repository) : INetWorthSnapshotService
{
    private readonly INetWorthSnapshotRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task PersistSnapshotAsync(Guid userId, NetWorthSnapshotData data, CancellationToken ct = default)
    {
        if (await _repository.ExistsAsync(userId, data.SnapshotDate, ct))
            return;

        var snapshot = new NetWorthSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SnapshotDate = data.SnapshotDate,
            BankingTotal = data.BankingTotal,
            BrokerageTotal = data.BrokerageTotal,
            CryptoTotal = data.CryptoTotal,
            TotalNetWorth = data.BankingTotal + data.BrokerageTotal + data.CryptoTotal,
            Currency = data.Currency,
            TakenAt = DateTimeOffset.UtcNow,
        };

        await _repository.PersistAsync(snapshot, ct);
    }

    public async Task<bool> HasSnapshotForCurrentMonthAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthEnd = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        return await _repository.ExistsAsync(userId, monthEnd, ct);
    }
}
