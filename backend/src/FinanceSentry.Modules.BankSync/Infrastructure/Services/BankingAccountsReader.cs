namespace FinanceSentry.Modules.BankSync.Infrastructure.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;

using FinanceSentry.Modules.BankSync.Domain.Repositories;

public class BankingAccountsReader(IBankAccountRepository accounts) : IBankingAccountsReader
{
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));

    public async Task<IReadOnlyList<BankingAccountSummary>> GetAccountSummariesAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _accounts.GetByUserIdAsync(userId, ct);

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
            a.UpdatedAt))
        .ToList();
    }
}
