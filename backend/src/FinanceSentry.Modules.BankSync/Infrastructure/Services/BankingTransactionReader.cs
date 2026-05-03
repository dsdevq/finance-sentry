namespace FinanceSentry.Modules.BankSync.Infrastructure.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

public class BankingTransactionReader(
    IBankAccountRepository accounts,
    ITransactionRepository transactions) : IBankingTransactionReader
{
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));

    public async Task<IReadOnlyList<BankingTransactionSummary>> GetTransactionsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var providerByAccount = accountList.ToDictionary(a => a.Id, a => a.Provider);

        var txList = await _transactions.GetByUserIdAsync(userId, ct);

        return txList
            .Where(t => t.IsActive)
            .Select(t =>
            {
                var effectiveDate = t.PostedDate ?? t.TransactionDate;
                return new { t, effectiveDate };
            })
            .Where(x => x.effectiveDate >= fromUtc && x.effectiveDate <= toUtc)
            .Select(x => new BankingTransactionSummary(
                x.t.AccountId,
                providerByAccount.TryGetValue(x.t.AccountId, out var p) ? p : "unknown",
                x.t.TransactionType ?? "debit",
                x.t.Amount,
                x.effectiveDate,
                x.t.IsPending))
            .ToList();
    }
}
