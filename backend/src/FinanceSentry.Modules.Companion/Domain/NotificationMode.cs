namespace FinanceSentry.Modules.Companion.Domain;

/// <summary>
/// How proactive the companion agent is (feature 031). Governs ONLY proactive outreach — on-demand
/// chat is always available. Default <see cref="Scan"/> preserves today's periodic behavior.
/// </summary>
public enum NotificationMode
{
    /// <summary>No proactive outreach; events recorded only.</summary>
    Quiet,

    /// <summary>One consolidated roll-up per day.</summary>
    Digest,

    /// <summary>Periodic material-event briefs (today's default) — agent pulls on its scan.</summary>
    Scan,

    /// <summary>Push the moment a material event fires (outbound wake).</summary>
    Realtime,
}
