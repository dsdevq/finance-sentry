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

        // Never let a stale/missing/failed sync write a $0 or stale sleeve as if it were
        // real movement. When a sleeve isn't trustworthy this run, carry forward its last
        // known-good value and record which sleeves were estimated so trend analysis can
        // tell a measured net worth from a partially carried-forward one.
        var previous = await _repository.GetLatestByUserIdAsync(userId, ct);
        var stale = new List<string>();

        var banking = ResolveSleeve("banking", data.BankingTotal, isFresh: true, previous?.BankingTotal, stale);
        var brokerage = ResolveSleeve("brokerage", data.BrokerageTotal, data.BrokerageFresh, previous?.BrokerageTotal, stale);
        var crypto = ResolveSleeve("crypto", data.CryptoTotal, data.CryptoFresh, previous?.CryptoTotal, stale);

        var snapshot = new NetWorthSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SnapshotDate = data.SnapshotDate,
            BankingTotal = banking,
            BrokerageTotal = brokerage,
            CryptoTotal = crypto,
            TotalNetWorth = banking + brokerage + crypto,
            Currency = data.Currency,
            TakenAt = DateTimeOffset.UtcNow,
            StaleSleeves = stale.Count > 0 ? string.Join(',', stale) : null,
        };

        await _repository.PersistAsync(snapshot, ct);
    }

    /// <summary>
    /// Returns the value to record for a sleeve. Uses the fresh value when the feed is
    /// trustworthy; otherwise carries forward the previous snapshot's value (and flags the
    /// sleeve as stale). Treats a drop to exactly $0 when we previously held value as a
    /// failed sync rather than a genuine liquidation.
    /// </summary>
    private static decimal ResolveSleeve(string name, decimal freshValue, bool isFresh, decimal? previousValue, List<string> stale)
    {
        var looksLikeFailedSync = freshValue == 0m && previousValue is > 0m;

        if (isFresh && !looksLikeFailedSync)
            return freshValue;

        if (previousValue is null)
            return freshValue; // no history to fall back on — best effort with what we have

        stale.Add(name);
        return previousValue.Value;
    }

    public async Task<bool> HasSnapshotForTodayAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _repository.ExistsAsync(userId, today, ct);
    }
}
