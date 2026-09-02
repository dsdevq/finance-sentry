namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

// ── Public result types ────────────────────────────────────────────────────────

/// <summary>
/// Net income and expense (in USD) produced by one counterparty in a given month,
/// after netting credits against debits. Offsetting gross movement stays TRANSFER
/// and does not appear here.
/// </summary>
public record CounterpartyMonthlyFlow(
    string Month,
    string CounterpartyName,
    string FlowRole,
    decimal NetIncomeUsd,
    decimal NetExpenseUsd);

/// <summary>
/// Result of counterparty classification over a transaction batch.
/// </summary>
public record CounterpartyClassificationResult(
    HashSet<Guid> MatchedTransactionIds,
    IReadOnlyList<CounterpartyMonthlyFlow> MonthlyFlows);

// ── Interface ──────────────────────────────────────────────────────────────────

/// <summary>
/// Matches transactions against known counterparties and computes monthly
/// net income / expense from the matched traffic.
/// </summary>
public interface ICounterpartyClassificationService
{
    /// <summary>
    /// Identifies which transactions belong to a known counterparty and returns:
    /// <list type="bullet">
    ///   <item>The union of matched transaction IDs (to exclude from normal flow).</item>
    ///   <item>Per-counterparty, per-month net income / expense in USD.</item>
    /// </list>
    /// Netting rule: for each (counterparty, month) group —
    /// netIncome = max(0, totalCredits − totalDebits);
    /// netExpense = max(0, totalDebits − totalCredits).
    /// </summary>
    Task<CounterpartyClassificationResult> ClassifyAsync(
        Guid userId,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, string> accountCurrencies,
        CancellationToken ct = default);
}

// ── Match-type constants ───────────────────────────────────────────────────────

internal static class MatchTypes
{
    internal const string DescriptionContains = "description_contains";
    internal const string MerchantNameContains = "merchant_name_contains";
}

// ── Implementation ─────────────────────────────────────────────────────────────

/// <inheritdoc />
public class CounterpartyClassificationService(ICounterpartyRepository counterparties) : ICounterpartyClassificationService
{
    private readonly ICounterpartyRepository _counterparties =
        counterparties ?? throw new ArgumentNullException(nameof(counterparties));

    /// <inheritdoc />
    public async Task<CounterpartyClassificationResult> ClassifyAsync(
        Guid userId,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, string> accountCurrencies,
        CancellationToken ct = default)
    {
        var knownCounterparties = await _counterparties.GetForUserAsync(userId, ct);

        if (knownCounterparties.Count == 0 || transactions.Count == 0)
            return new CounterpartyClassificationResult([], []);

        var matchedIds = new HashSet<Guid>();
        // Key: (counterpartyName, month) → (totalCreditsUsd, totalDebitsUsd)
        var buckets = new Dictionary<(string Name, string FlowRole, string Month), (decimal Credits, decimal Debits)>();

        foreach (var tx in transactions)
        {
            if (!tx.IsActive)
                continue;

            var matched = FindCounterparty(tx, knownCounterparties);
            if (matched is null)
                continue;

            matchedIds.Add(tx.Id);

            var currency = accountCurrencies.TryGetValue(tx.AccountId, out var cur) ? cur : "USD";
            var amountUsd = CurrencyConverter.ToUsd(tx.Amount, currency);
            var month = (tx.PostedDate ?? tx.TransactionDate).ToString("yyyy-MM");
            var key = (matched.Name, matched.FlowRole, month);

            buckets.TryGetValue(key, out var existing);

            if (tx.TransactionType == "credit")
                buckets[key] = (existing.Credits + amountUsd, existing.Debits);
            else
                buckets[key] = (existing.Credits, existing.Debits + amountUsd);
        }

        var monthlyFlows = buckets
            .Select(kv => new CounterpartyMonthlyFlow(
                kv.Key.Month,
                kv.Key.Name,
                kv.Key.FlowRole,
                Math.Max(0m, kv.Value.Credits - kv.Value.Debits),
                Math.Max(0m, kv.Value.Debits - kv.Value.Credits)))
            .ToList();

        return new CounterpartyClassificationResult(matchedIds, monthlyFlows);
    }

    // Returns the first counterparty whose any rule matches the transaction.
    private static Counterparty? FindCounterparty(Transaction tx, IReadOnlyList<Counterparty> counterparties)
    {
        foreach (var cp in counterparties)
        {
            foreach (var rule in cp.Rules)
            {
                if (Matches(tx, rule))
                    return cp;
            }
        }
        return null;
    }

    private static bool Matches(Transaction tx, CounterpartyRule rule) =>
        rule.MatchType switch
        {
            MatchTypes.DescriptionContains =>
                tx.Description.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            MatchTypes.MerchantNameContains =>
                tx.MerchantName is not null &&
                tx.MerchantName.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
}
