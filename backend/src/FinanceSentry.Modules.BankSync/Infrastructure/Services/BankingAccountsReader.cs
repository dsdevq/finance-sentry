namespace FinanceSentry.Modules.BankSync.Infrastructure.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;

using FinanceSentry.Modules.BankSync.Domain.Repositories;

public class BankingAccountsReader(
    IBankAccountRepository accounts,
    ISyncJobRepository syncJobs) : IBankingAccountsReader
{
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly ISyncJobRepository _syncJobs = syncJobs ?? throw new ArgumentNullException(nameof(syncJobs));

    public async Task<IReadOnlyList<BankingAccountSummary>> GetAccountSummariesAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _accounts.GetByUserIdAsync(userId, ct);
        var lastSuccessful = await _syncJobs.GetLastSuccessfulSyncTimesByUserAsync(userId, ct);

        return list.Select(a => new BankingAccountSummary(
            a.Id,
            a.BankName,
            a.AccountType,
            a.AccountNumberLast4,
            a.Provider,
            a.Currency,
            a.CurrentBalance,
            a.CurrentBalance.HasValue ? CurrencyConverter.ToUsd(a.CurrentBalance.Value, a.Currency) : null,
            a.SyncStatus == "active" ? "synced" : a.SyncStatus,
            a.UpdatedAt,
            lastSuccessful.TryGetValue(a.Id, out var lastOk) ? lastOk : null,
            a.MonobankCredentialId,
            a.TrueLayerConnectionId,
            a.ProductType))
        .ToList();
    }
}
