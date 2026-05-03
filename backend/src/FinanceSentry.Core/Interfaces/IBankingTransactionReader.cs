namespace FinanceSentry.Core.Interfaces;

public interface IBankingTransactionReader
{
    Task<IReadOnlyList<BankingTransactionSummary>> GetTransactionsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public record BankingTransactionSummary(
    Guid AccountId,
    string Provider,
    string TransactionType,
    decimal Amount,
    DateTime EffectiveDate,
    bool IsPending);
