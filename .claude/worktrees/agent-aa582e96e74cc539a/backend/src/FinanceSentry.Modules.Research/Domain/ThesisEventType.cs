namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Lifecycle event type recorded for a thesis or candidate (FR-001). <c>Promoted</c>/<c>Rejected</c>/
/// <c>Expired</c> are 019 (Candidate) events, modeled now but only reachable once 019 ships.
/// </summary>
public enum ThesisEventType
{
    Created,
    Broken,
    Unbroken,
    Closed,
    Promoted,
    Rejected,
    Expired,
    Snapshot,
}
