namespace FinanceSentry.Modules.Companion.Domain;

/// <summary>
/// Lifecycle/outcome of a captured event (feature 031). Every captured event carries one — none are
/// lost (FR-007). Terminal: <see cref="Delivered"/>, <see cref="SuppressedByMode"/>, <see cref="Failed"/>.
/// Realtime <see cref="DeferredQuietHours"/>/<see cref="SuppressedByRateLimit"/> are re-evaluated next tick.
/// </summary>
public enum EventDisposition
{
    Pending,
    Dispatched,
    HeldForDigest,
    Delivered,
    SuppressedByMode,
    SuppressedByDedup,
    SuppressedByRateLimit,
    DeferredQuietHours,
    Failed,
}
