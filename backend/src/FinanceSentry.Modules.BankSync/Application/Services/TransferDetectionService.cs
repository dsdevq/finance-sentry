namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Detects likely internal transfers between two accounts of the same user.
/// </summary>
public interface ITransferDetectionService
{
    /// <summary>
    /// Returns true if the two transactions are likely an internal transfer pair.
    /// Criteria: same absolute amount (±0.01), dates within 2 days, different accounts,
    /// and at least one is type "transfer" or the descriptions share similarity.
    /// </summary>
    bool IsLikelyTransfer(Transaction debit, Transaction credit);

    /// <summary>
    /// Detects all likely internal transfers within a batch and returns the union of
    /// matched debit + credit transaction Ids. Each credit is consumed by at most one
    /// debit so an ambiguous credit is not double-counted. Pending / inactive rows are
    /// ignored — the caller does not need to pre-filter.
    /// </summary>
    HashSet<Guid> DetectTransferTransactionIds(IReadOnlyCollection<Transaction> transactions);
}

/// <inheritdoc />
public class TransferDetectionService : ITransferDetectionService
{
    private const decimal AmountTolerance = 0.01m;
    private const int MaxDateDifferenceInDays = 2;

    /// <inheritdoc />
    public bool IsLikelyTransfer(Transaction debit, Transaction credit)
    {
        if (debit == null) throw new ArgumentNullException(nameof(debit));
        if (credit == null) throw new ArgumentNullException(nameof(credit));

        // Must belong to the same user but different accounts
        if (debit.UserId != credit.UserId)
            return false;
        if (debit.AccountId == credit.AccountId)
            return false;

        // Amounts must match within tolerance
        if (Math.Abs(debit.Amount - credit.Amount) > AmountTolerance)
            return false;

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

        // Fallback: check description similarity (shared significant words)
        return HaveSimilarDescriptions(debit.Description, credit.Description);
    }

    /// <inheritdoc />
    public HashSet<Guid> DetectTransferTransactionIds(IReadOnlyCollection<Transaction> transactions)
    {
        if (transactions == null) throw new ArgumentNullException(nameof(transactions));

        var matched = new HashSet<Guid>();
        if (transactions.Count == 0)
            return matched;

        var debits = new List<Transaction>();
        var credits = new List<Transaction>();
        foreach (var tx in transactions)
        {
            if (tx.IsPending || !tx.IsActive) continue;
            if (tx.TransactionType == "debit") debits.Add(tx);
            else if (tx.TransactionType == "credit") credits.Add(tx);
        }

        if (debits.Count == 0 || credits.Count == 0)
            return matched;

        var consumedCredits = new HashSet<Guid>();
        foreach (var debit in debits)
        {
            foreach (var credit in credits)
            {
                if (consumedCredits.Contains(credit.Id)) continue;
                if (!IsLikelyTransfer(debit, credit)) continue;

                matched.Add(debit.Id);
                matched.Add(credit.Id);
                consumedCredits.Add(credit.Id);
                break;
            }
        }

        return matched;
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
