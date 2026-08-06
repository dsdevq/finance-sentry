namespace FinanceSentry.Modules.BankSync.Domain.Interfaces;

using FinanceSentry.Modules.BankSync.Application.Services;

public record BankAccountInfo(
    string ExternalAccountId,
    string Name,
    string AccountType,
    string AccountNumberLast4,
    decimal? CurrentBalance,
    string Currency,
    string OwnerName);

public interface IBankProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<BankAccountInfo>> GetAccountsAsync(string credential, CancellationToken ct = default);

    Task<(IReadOnlyList<TransactionCandidate> Candidates, DateTime? NextSyncFrom)> SyncTransactionsAsync(
        string credential, string externalAccountId, Guid accountId, Guid userId,
        DateTime? since, CancellationToken ct = default);

    Task DisconnectAsync(string credential, CancellationToken ct = default);
}

public interface IBankProviderFactory
{
    IBankProvider Resolve(string provider);
}
