namespace FinanceSentry.Core.Interfaces;

public interface IActiveSubscriptionsReader
{
    Task<IReadOnlyList<ActiveSubscriptionSummary>> GetActiveSubscriptionsAsync(
        Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Normalized merchant keys of every ACTIVE detected commitment — subscriptions and
    /// installments alike, unlike <see cref="GetActiveSubscriptionsAsync"/> which is scoped to
    /// recurring services for cash-flow projection. The keys are the ones the detector stored,
    /// so callers match against them by deriving the same key from a transaction (see
    /// <c>MerchantNameNormalizer.NormalizeDetectionKey</c>). Comparison is ordinal — both sides
    /// are already lower-cased by normalization.
    /// </summary>
    Task<IReadOnlySet<string>> GetActiveCommitmentMerchantKeysAsync(
        Guid userId, CancellationToken ct = default);
}

public record ActiveSubscriptionSummary(
    string MerchantNameDisplay,
    string Cadence,
    decimal AverageAmount,
    string Currency,
    DateOnly NextExpectedDate);
