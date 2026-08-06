namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Per-source durable failure counters for registered news sources (feature 030, FR-009). Unlike the
/// analyst-source in-memory counters, these live on the <see cref="NewsSource"/> row and survive
/// restarts. A sync-failure alert fires once when the count reaches <see cref="AlertThreshold"/>
/// consecutive failures; a source that keeps failing to <see cref="DisableThreshold"/> is auto-disabled
/// so it stops retrying (and log-spamming) every run. Success resets the counter.
/// </summary>
public static class NewsSourceHealthTracker
{
    public const int AlertThreshold = 2;

    /// <summary>Consecutive failures before a source is retired. ~6h at the 30-min ingestion cadence.</summary>
    public const int DisableThreshold = 12;

    public static void RecordSuccess(NewsSource source)
    {
        source.ConsecutiveFailures = 0;
        source.LastSuccessAt = DateTimeOffset.UtcNow;
        source.LastFailureReason = null;
    }

    /// <summary>
    /// Records a failure and reports what follow-up action the caller should take. <see cref="NewsSourceFailureOutcome.Alert"/>
    /// fires exactly once (at the threshold) so a stuck source does not re-alert every run;
    /// <see cref="NewsSourceFailureOutcome.Disable"/> flips <see cref="NewsSource.Enabled"/> off so the
    /// source drops out of the ingestion loop entirely.
    /// </summary>
    public static NewsSourceFailureOutcome RecordFailure(NewsSource source, string reason)
    {
        source.ConsecutiveFailures++;
        source.LastFailureReason = reason;

        if (source.Enabled && source.ConsecutiveFailures >= DisableThreshold)
        {
            source.Enabled = false;
            return NewsSourceFailureOutcome.Disable;
        }

        return source.ConsecutiveFailures == AlertThreshold
            ? NewsSourceFailureOutcome.Alert
            : NewsSourceFailureOutcome.None;
    }
}

/// <summary>Follow-up action a caller should take after recording a news-source failure.</summary>
public enum NewsSourceFailureOutcome
{
    /// <summary>Nothing to do — failure recorded, below the alert threshold or already alerted.</summary>
    None,

    /// <summary>The source just reached the consecutive-failure alert threshold; notify once.</summary>
    Alert,

    /// <summary>The source was auto-disabled after sustained failures; notify and stop retrying it.</summary>
    Disable,
}
