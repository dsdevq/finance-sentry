namespace FinanceSentry.Modules.Companion.Application.Services;

using FinanceSentry.Modules.Companion.Domain;

/// <summary>
/// The Finance-Sentry-owned policy for what counts as a proactively-notifiable event and how it maps
/// to the current mode (feature 031). Pure — no I/O — so it is unit-testable in isolation.
/// </summary>
public interface IMaterialityPolicy
{
    /// <summary>Maps an alert type to a companion event kind, or null if it is not surfaced.</summary>
    CompanionEventKind? ClassifyAlert(string alertType);

    /// <summary>The disposition a freshly-captured event gets under the given mode.</summary>
    EventDisposition DispositionForMode(NotificationMode mode);

    string AlertDedupKey(Guid alertId);

    string AnalystDedupKey(Guid userId, Guid analystActionId);
}
