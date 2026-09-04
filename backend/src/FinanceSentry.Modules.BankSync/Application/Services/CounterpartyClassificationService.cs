namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

// ── Public result types ────────────────────────────────────────────────────────

/// <summary>
/// Gross inbound and outbound movement (in USD) between the user and one counterparty
/// in a given month. Each direction is reported whole: a rent credit and a support debit
/// in the same month with the same counterparty are two separate facts, never one net
/// figure. See <see cref="ICounterpartyClassificationService"/> for why.
/// </summary>
public record CounterpartyMonthlyFlow(
    string Month,
    string CounterpartyName,
    string FlowRole,
    decimal InflowUsd,
    decimal OutflowUsd);

/// <summary>
/// Result of counterparty classification over a transaction batch.
/// </summary>
public record CounterpartyClassificationResult(
    HashSet<Guid> MatchedTransactionIds,
    IReadOnlyList<CounterpartyMonthlyFlow> MonthlyFlows);

// ── Interface ──────────────────────────────────────────────────────────────────

/// <summary>
/// Matches transactions against known counterparties and reports the monthly gross
/// movement in each direction.
/// <para>
/// Classification is per DIRECTION, with no per-counterparty netting: every rent credit
/// is income and every family-support debit is an expense, even when both involve the same
/// counterparty in the same month. Netting the two hid the pair — a month where ₴18k of rent
/// arrived and ₴13k of support went back out reported ₴5k of income and no spending at all,
/// which is the same transfer-blind savings rate this feature exists to fix.
/// </para>
/// </summary>
public interface ICounterpartyClassificationService
{
    /// <summary>
    /// Classifies the user's whole statistics window in one pass. This is the single
    /// entry point every consumer of the classification (money flow, savings rate,
    /// top categories) shares: the result is computed once per request and handed to
    /// each of them, so they can never disagree about what a counterparty movement was.
    /// </summary>
    Task<CounterpartyClassificationResult> ClassifyForWindowAsync(
        Guid userId,
        int months,
        CancellationToken ct = default);

    /// <summary>
    /// Identifies which transactions belong to a known counterparty and returns:
    /// <list type="bullet">
    ///   <item>The union of matched transaction IDs (to exclude from normal flow).</item>
    ///   <item>Per-counterparty, per-month gross inflow / outflow in USD.</item>
    /// </list>
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
public class CounterpartyClassificationService(
    ICounterpartyRepository counterparties,
    ITransactionRepository transactions,
    IBankAccountRepository accounts) : ICounterpartyClassificationService
{
    private readonly ICounterpartyRepository _counterparties =
        counterparties ?? throw new ArgumentNullException(nameof(counterparties));
    private readonly ITransactionRepository _transactions =
        transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly IBankAccountRepository _accounts =
        accounts ?? throw new ArgumentNullException(nameof(accounts));

    /// <inheritdoc />
    public async Task<CounterpartyClassificationResult> ClassifyForWindowAsync(
        Guid userId,
        int months,
        CancellationToken ct = default)
    {
        var accountList = await _accounts.GetByUserIdAsync(userId, ct);
        var accountCurrencies = accountList
            .Where(a => a.IsActive)
            .ToDictionary(a => a.Id, a => a.Currency);

        var txList = (await _transactions.GetByUserIdSinceAsync(
            userId, MonthWindow.StartOfMonthsAgo(months), ct)).ToList();

        return await ClassifyAsync(userId, txList, accountCurrencies, ct);
    }

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
        // Key: (counterpartyName, flowRole, month) → (grossInflowUsd, grossOutflowUsd)
        var buckets = new Dictionary<(string Name, string FlowRole, string Month), (decimal Inflow, decimal Outflow)>();

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
                buckets[key] = (existing.Inflow + amountUsd, existing.Outflow);
            else
                buckets[key] = (existing.Inflow, existing.Outflow + amountUsd);
        }

        var monthlyFlows = buckets
            .Select(kv => new CounterpartyMonthlyFlow(
                kv.Key.Month,
                kv.Key.Name,
                kv.Key.FlowRole,
                kv.Value.Inflow,
                kv.Value.Outflow))
            .OrderBy(f => f.Month, StringComparer.Ordinal)
            .ThenBy(f => f.CounterpartyName, StringComparer.Ordinal)
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
