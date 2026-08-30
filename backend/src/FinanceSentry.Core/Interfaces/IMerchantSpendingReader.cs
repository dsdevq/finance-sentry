namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Cross-module read port over BankSync's per-category spend aggregation (039 pattern).
/// Budgets consumes this instead of referencing BankSync directly.
/// </summary>
public interface IMerchantSpendingReader
{
    /// <summary>
    /// Debit spend for the window, grouped by merchant category key, each group summed in USD
    /// (converted per account currency at read time — never native amounts).
    /// </summary>
    Task<IReadOnlyDictionary<string, decimal>> GetSpendingByCategoryUsdAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
