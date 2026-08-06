namespace FinanceSentry.Core.Interfaces;

public interface IBankingTransactionReader
{
    Task<IReadOnlyList<BankingTransactionSummary>> GetTransactionsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

/// <param name="Amount">Native amount in the account's own currency — never sum this across accounts.</param>
/// <param name="Currency">ISO code of the account the transaction belongs to.</param>
/// <param name="AmountUsd">
/// <paramref name="Amount"/> converted to USD at read time. Aggregations MUST sum this, not
/// <paramref name="Amount"/>, so totals never mix currencies.
/// </param>
public record BankingTransactionSummary(
    Guid AccountId,
    string Provider,
    string TransactionType,
    decimal Amount,
    string Currency,
    decimal AmountUsd,
    DateTime EffectiveDate,
    bool IsPending);
