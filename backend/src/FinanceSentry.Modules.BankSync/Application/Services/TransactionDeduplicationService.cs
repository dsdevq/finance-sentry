namespace FinanceSentry.Modules.BankSync.Application.Services;

using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Deduplicates incoming transactions against existing records using HMAC-SHA256 hashes.
///
/// FR-006: Detect and filter duplicate transactions.
/// SC-005: 95%+ deduplication accuracy required.
///
/// Hash formula: HMAC-SHA256(accountId|amount|date|description, masterKey)
/// - Deterministic: same inputs always produce the same hash.
/// - Pending and posted versions of the same transaction are treated as DIFFERENT
///   because their dates differ (pending_date vs posted_date).
/// </summary>
public interface ITransactionDeduplicationService
{
    /// <summary>
    /// Computes the unique deduplication hash for a transaction.
    /// </summary>
    string ComputeHash(Guid accountId, decimal amount, DateTime date, string description);

    /// <summary>
    /// Filters a list of incoming transactions, returning only those whose hashes
    /// are NOT present in <paramref name="existingHashes"/>.
    /// </summary>
    IReadOnlyList<TransactionCandidate> FilterDuplicates(
        IEnumerable<TransactionCandidate> incoming,
        IReadOnlySet<string> existingHashes);

    /// <summary>Converts a deduplicated candidate into a Transaction entity ready for insertion.</summary>
    Transaction ToEntity(TransactionCandidate candidate);
}

/// <summary>
/// A candidate transaction to be deduplicated before insertion.
/// </summary>
public record TransactionCandidate(
    Guid AccountId,
    Guid UserId,
    decimal Amount,
    DateTime TransactionDate,
    DateTime? PostedDate,
    string Description,
    bool IsPending,
    string? TransactionType,
    string? MerchantName,
    string? MerchantCategory,
    string? PlaidTransactionId
)
{
    /// <summary>
    /// The date used for hashing. Pending transactions use TransactionDate;
    /// posted transactions use PostedDate when available.
    /// </summary>
    public DateTime HashDate => (!IsPending && PostedDate.HasValue) ? PostedDate.Value : TransactionDate;
}

/// <inheritdoc />
public class TransactionDeduplicationService : ITransactionDeduplicationService
{
    private readonly byte[] _masterKey;

    /// <param name="masterKeyBase64">
    /// Base64-encoded HMAC key. Must be the same key used when hashes were originally stored.
    /// Changing this key invalidates all existing hashes.
    /// </param>
    public TransactionDeduplicationService(string masterKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(masterKeyBase64))
            throw new ArgumentException("Master key cannot be empty.", nameof(masterKeyBase64));

        _masterKey = Convert.FromBase64String(masterKeyBase64);
    }

    /// <inheritdoc />
    public string ComputeHash(Guid accountId, decimal amount, DateTime date, string description)
    {
        // Canonical representation: lowercase, trimmed, fixed decimal format
        var payload = $"{accountId}|{amount:F4}|{date:yyyy-MM-dd}|{description.Trim().ToLowerInvariant()}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(_masterKey);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant(); // 64 hex chars
    }

    /// <inheritdoc />
    public IReadOnlyList<TransactionCandidate> FilterDuplicates(
        IEnumerable<TransactionCandidate> incoming,
        IReadOnlySet<string> existingHashes)
    {
        var result = new List<TransactionCandidate>();
        var seenThisBatch = new HashSet<string>(); // guard against duplicates within the same batch

        foreach (var candidate in incoming)
        {
            var hash = ComputeHash(
                candidate.AccountId,
                candidate.Amount,
                candidate.HashDate,
                candidate.Description);

            // Skip if already in DB or already seen in this batch
            if (existingHashes.Contains(hash) || !seenThisBatch.Add(hash))
                continue;

            result.Add(candidate);
        }

        return result;
    }

    /// <summary>
    /// Converts a <see cref="TransactionCandidate"/> to a <see cref="Transaction"/> entity,
    /// computing and setting the <see cref="Transaction.UniqueHash"/> automatically.
    /// </summary>
    public Transaction ToEntity(TransactionCandidate candidate)
    {
        var hash = ComputeHash(
            candidate.AccountId,
            candidate.Amount,
            candidate.HashDate,
            candidate.Description);

        return new Transaction(candidate.AccountId, candidate.UserId, candidate.Amount,
            candidate.TransactionDate, candidate.Description, hash, candidate.IsPending)
        {
            PostedDate = candidate.PostedDate,
            TransactionType = candidate.TransactionType,
            MerchantName = candidate.MerchantName,
            MerchantCategory = candidate.MerchantCategory
        };
    }
}
