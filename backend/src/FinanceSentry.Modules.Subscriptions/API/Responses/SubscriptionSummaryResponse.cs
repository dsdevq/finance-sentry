namespace FinanceSentry.Modules.Subscriptions.API.Responses;

/// <summary>
/// Committed recurring spend for one bucket (subscriptions, installments, or both),
/// with every amount already converted to the response currency.
/// </summary>
/// <param name="Monthly">Current monthly run-rate.</param>
/// <param name="Next12Months">
/// What actually leaves the account over the next 12 months. Subscriptions are open-ended
/// (<c>Monthly × 12</c>); an installment contributes only its remaining payments, capped at
/// 12 — a plan with one payment left costs one payment, not a year of them.
/// </param>
/// <param name="RemainingCommitment">
/// Total still owed until every plan in the bucket ends. Null where that has no meaning:
/// open-ended subscriptions, and the combined bucket that mixes them with finite plans.
/// </param>
/// <param name="HasUnknownSchedule">
/// True when the bucket holds a plan with neither a term nor an end date. Such a plan is
/// assumed to keep running for <see cref="Next12Months"/> and is left out of
/// <see cref="RemainingCommitment"/>, so treat both as approximate.
/// </param>
public record SpendBucketResponse(
    decimal Monthly,
    decimal Next12Months,
    decimal? RemainingCommitment,
    int ActiveCount,
    bool HasUnknownSchedule);

public record SubscriptionSummaryResponse(
    SpendBucketResponse Subscriptions,
    SpendBucketResponse Installments,
    SpendBucketResponse Combined,
    int PotentiallyCancelledCount,
    string Currency);
