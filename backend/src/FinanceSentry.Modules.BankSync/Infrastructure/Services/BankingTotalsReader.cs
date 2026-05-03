namespace FinanceSentry.Modules.BankSync.Infrastructure.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

public class BankingTotalsReader(IBankAccountRepository accounts) : IBankingTotalsReader
{
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));

    public async Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(CancellationToken ct = default)
    {
        var active = await _accounts.GetAllActiveAsync(ct);
        return active.Select(a => a.UserId).Distinct().ToList();
    }

    public async Task<decimal> GetTotalUsdAsync(Guid userId, CancellationToken ct = default)
    {
        var accounts = await _accounts.GetByUserIdAsync(userId, ct);
        return accounts
            .Where(a => a.IsActive && a.CurrentBalance.HasValue)
            .Sum(a => CurrencyConverter.ToUsd(a.CurrentBalance!.Value, a.Currency));
    }
}
