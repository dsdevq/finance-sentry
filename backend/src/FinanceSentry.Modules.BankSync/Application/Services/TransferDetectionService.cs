namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Core.Domain;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain;

/// <summary>A matched internal-transfer pair: the sending (debit) and receiving (credit) leg.</summary>
public readonly record struct TransferPair(Transaction Debit, Transaction Credit);

/// <summary>
/// Detects likely internal transfers between two accounts of the same user.
/// </summary>
public interface ITransferDetectionService
{
    /// <summary>
    /// Returns true if the two transactions are likely an internal transfer pair.
    /// Criteria: same absolute amount (±0.01), dates within 2 days, different accounts,
    /// and at least one is type "transfer" or the descriptions share similarity.
    /// Without currency information only same-currency (exact-amount) pairs can match.
    /// <paramref name="crossCurrencyTolerance"/> overrides the default relative tolerance
    /// applied when comparing USD-converted cross-currency amounts (the FX-spread sentinel
    /// widens it — a conversion losing a large spread still has to pair).
    /// </summary>
    bool IsLikelyTransfer(
        Transaction debit, Transaction credit,
        string? debitCurrency = null, string? creditCurrency = null,
        decimal? crossCurrencyTolerance = null);

    /// <summary>
    /// Detects all likely internal transfers within a batch and returns the union of
    /// matched debit + credit transaction Ids. Each credit is consumed by at most one
    /// debit so an ambiguous credit is not double-counted. Inactive rows are ignored —
    /// the caller does not need to pre-filter. Pending rows DO participate: cash-flow
    /// statistics count pending money, so a transfer whose leg is still pending must be
    /// excluded like any other or it leaks into income/spending.
    /// When <paramref name="accountCurrencies"/> is provided, cross-currency pairs
    /// (e.g. a EUR debit funding a UAH credit) are also matched by comparing the
    /// USD-converted amounts within an FX tolerance.
    /// </summary>
    HashSet<Guid> DetectTransferTransactionIds(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyDictionary<Guid, string>? accountCurrencies = null);

    /// <summary>
    /// Same matching pipeline as <see cref="DetectTransferTransactionIds"/> but returns the
    /// matched debit→credit pairs themselves, for callers that need per-pair figures (e.g.
    /// the FX-spread sentinel computing an implied conversion rate per pair).
    /// <paramref name="crossCurrencyTolerance"/> is passed through to
    /// <see cref="IsLikelyTransfer"/>.
    /// </summary>
    IReadOnlyList<TransferPair> DetectTransferPairs(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyDictionary<Guid, string>? accountCurrencies = null,
        decimal? crossCurrencyTolerance = null);
}

/// <inheritdoc />
public class TransferDetectionService : ITransferDetectionService
{
    private const decimal AmountTolerance = 0.01m;
    private const int MaxDateDifferenceInDays = 2;

    // Cross-currency legs never match exactly: the sending and receiving banks apply their
    // own rates and spreads, and our rate table refreshes on its own schedule. 5% absorbs
    // typical retail FX spread + a day of rate drift without pairing unrelated amounts.
    private const decimal CrossCurrencyRelativeTolerance = 0.05m;

    /// <inheritdoc />
    public bool IsLikelyTransfer(
        Transaction debit, Transaction credit,
        string? debitCurrency = null, string? creditCurrency = null,
        decimal? crossCurrencyTolerance = null)
    {
        if (debit == null) throw new ArgumentNullException(nameof(debit));
        if (credit == null) throw new ArgumentNullException(nameof(credit));

        // Must belong to the same user but different accounts
        if (debit.UserId != credit.UserId)
            return false;
        if (debit.AccountId == credit.AccountId)
            return false;

        // Without both currencies the legs are assumed same-currency (legacy behaviour).
        var sameCurrency = debitCurrency is null || creditCurrency is null
            || string.Equals(debitCurrency, creditCurrency, StringComparison.OrdinalIgnoreCase);

        if (sameCurrency)
        {
            // Amounts must match within tolerance
            if (Math.Abs(debit.Amount - credit.Amount) > AmountTolerance)
                return false;
        }
        else if (!AmountsMatchAcrossCurrencies(
            debit.Amount, debitCurrency!, credit.Amount, creditCurrency!,
            crossCurrencyTolerance ?? CrossCurrencyRelativeTolerance))
        {
            return false;
        }

        // Dates must be within 2 calendar days of each other
        var debitDate  = (debit.PostedDate  ?? debit.TransactionDate).Date;
        var creditDate = (credit.PostedDate ?? credit.TransactionDate).Date;
        if (Math.Abs((debitDate - creditDate).TotalDays) > MaxDateDifferenceInDays)
            return false;

        // At least one is explicitly typed as transfer OR descriptions have similarity
        var eitherIsTransferType =
            debit.TransactionType?.Equals("transfer", StringComparison.OrdinalIgnoreCase) == true ||
            credit.TransactionType?.Equals("transfer", StringComparison.OrdinalIgnoreCase) == true;

        if (eitherIsTransferType)
            return true;

        // Cross-currency legs live at different banks, often in different languages
        // ("To UAH account" vs «Від: …»), so shared wording is rare. A transfer category
        // on either leg (MCC 4829, directional-prefix classification, …) is accepted as
        // the confirming signal instead — the categorised leg is already excluded from
        // cash-flow, and the match extends that exclusion to its uncategorised twin.
        if (!sameCurrency &&
            (CategoryKeys.IsTransfer(debit.MerchantCategory) || CategoryKeys.IsTransfer(credit.MerchantCategory)))
        {
            return true;
        }

        // Fallback: check description similarity (shared significant words)
        return HaveSimilarDescriptions(debit.Description, credit.Description);
    }

    /// <inheritdoc />
    public HashSet<Guid> DetectTransferTransactionIds(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyDictionary<Guid, string>? accountCurrencies = null)
    {
        var matched = new HashSet<Guid>();
        foreach (var pair in DetectTransferPairs(transactions, accountCurrencies))
        {
            matched.Add(pair.Debit.Id);
            matched.Add(pair.Credit.Id);
        }

        return matched;
    }

    /// <inheritdoc />
    public IReadOnlyList<TransferPair> DetectTransferPairs(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyDictionary<Guid, string>? accountCurrencies = null,
        decimal? crossCurrencyTolerance = null)
    {
        if (transactions == null) throw new ArgumentNullException(nameof(transactions));

        var pairs = new List<TransferPair>();
        if (transactions.Count == 0)
            return pairs;

        var debits = new List<Transaction>();
        var credits = new List<Transaction>();
        foreach (var tx in transactions)
        {
            if (!tx.IsActive) continue;
            var txType = tx.TransactionType?.Trim();
            if (txType?.Equals("debit", StringComparison.OrdinalIgnoreCase) == true) debits.Add(tx);
            else if (txType?.Equals("credit", StringComparison.OrdinalIgnoreCase) == true) credits.Add(tx);
        }

        if (debits.Count == 0 || credits.Count == 0)
            return pairs;

        string? CurrencyOf(Transaction tx) =>
            accountCurrencies is not null && accountCurrencies.TryGetValue(tx.AccountId, out var currency)
                ? currency
                : null;

        var consumedCredits = new HashSet<Guid>();
        foreach (var debit in debits)
        {
            foreach (var credit in credits)
            {
                if (consumedCredits.Contains(credit.Id)) continue;
                if (!IsLikelyTransfer(debit, credit, CurrencyOf(debit), CurrencyOf(credit), crossCurrencyTolerance)) continue;

                pairs.Add(new TransferPair(debit, credit));
                consumedCredits.Add(credit.Id);
                break;
            }
        }

        return pairs;
    }

    /// <summary>
    /// Compares two amounts in different currencies by converting both to USD. Unknown
    /// currencies never match: <see cref="CurrencyConverter.ToUsd"/> silently falls back
    /// to 1:1 for them, which would compare unrelated magnitudes as if equal.
    /// </summary>
    private static bool AmountsMatchAcrossCurrencies(
        decimal debitAmount, string debitCurrency, decimal creditAmount, string creditCurrency,
        decimal relativeTolerance)
    {
        if (!CurrencyConverter.IsKnown(debitCurrency) || !CurrencyConverter.IsKnown(creditCurrency))
            return false;

        var debitUsd = CurrencyConverter.ToUsd(debitAmount, debitCurrency);
        var creditUsd = CurrencyConverter.ToUsd(creditAmount, creditCurrency);
        var larger = Math.Max(debitUsd, creditUsd);
        if (larger <= 0)
            return false;

        return Math.Abs(debitUsd - creditUsd) / larger <= relativeTolerance;
    }

    private static bool HaveSimilarDescriptions(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        var wordsA = a.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var wordsB = b.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // At least 2 words in common (ignoring very short words)
        var commonWords = wordsB
            .Where(w => w.Length > 2 && wordsA.Contains(w))
            .Count();

        return commonWords >= 2;
    }
}
