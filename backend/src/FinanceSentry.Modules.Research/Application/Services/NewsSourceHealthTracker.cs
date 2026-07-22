namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Per-source durable failure counters for registered news sources (feature 030, FR-009). Unlike the
/// analyst-source in-memory counters, these live on the <see cref="NewsSource"/> row and survive
/// restarts. A sync-failure alert fires once the count reaches <see cref="AlertThreshold"/> consecutive
/// failures; success resets the counter.
/// </summary>
public static class NewsSourceHealthTracker
{
    public const int AlertThreshold = 2;

    public static void RecordSuccess(NewsSource source)
    {
        source.ConsecutiveFailures = 0;
        source.LastSuccessAt = DateTimeOffset.UtcNow;
        source.LastFailureReason = null;
    }

    /// <summary>Records a failure and returns <c>true</c> when the alert threshold is reached.</summary>
    public static bool RecordFailure(NewsSource source, string reason)
    {
        source.ConsecutiveFailures++;
        source.LastFailureReason = reason;
        return source.ConsecutiveFailures >= AlertThreshold;
    }
}
