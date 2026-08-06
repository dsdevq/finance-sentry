namespace FinanceSentry.Core.Interfaces;

public interface INetWorthSnapshotService
{
    Task PersistSnapshotAsync(
        Guid userId,
        NetWorthSnapshotData data,
        CancellationToken ct = default);

    Task<bool> HasSnapshotForTodayAsync(
        Guid userId,
        CancellationToken ct = default);
}

public record NetWorthSnapshotData(
    DateOnly SnapshotDate,
    decimal BankingTotal,
    decimal BrokerageTotal,
    decimal CryptoTotal,
    bool BankingFresh = true,
    bool BrokerageFresh = true,
    bool CryptoFresh = true,
    string Currency = "USD");
