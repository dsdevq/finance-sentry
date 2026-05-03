namespace FinanceSentry.Core.Interfaces;

public interface INetWorthSnapshotService
{
    Task PersistSnapshotAsync(
        Guid userId,
        NetWorthSnapshotData data,
        CancellationToken ct = default);

    Task<bool> HasSnapshotForCurrentMonthAsync(
        Guid userId,
        CancellationToken ct = default);

    Task ReplaceAllSnapshotsAsync(
        Guid userId,
        IReadOnlyList<NetWorthSnapshotData> snapshots,
        CancellationToken ct = default);
}

public record NetWorthSnapshotData(
    DateOnly SnapshotDate,
    decimal BankingTotal,
    decimal BrokerageTotal,
    decimal CryptoTotal,
    string Currency = "USD");
