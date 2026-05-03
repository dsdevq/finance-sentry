namespace FinanceSentry.Core.Interfaces;

public interface IBankingAccountsReader
{
    Task<IReadOnlyList<BankingAccountSummary>> GetAccountSummariesAsync(Guid userId, CancellationToken ct = default);
}

public record BankingAccountSummary(
    Guid AccountId,
    string BankName,
    string AccountType,
    string AccountNumberLast4,
    string Provider,
    string Currency,
    decimal? CurrentBalance,
    decimal? BalanceUsd,
    string SyncStatus,
    DateTime? LastSyncTimestamp);
