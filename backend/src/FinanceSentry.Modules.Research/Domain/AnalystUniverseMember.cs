namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// A ticker included in the analyst-actions ingestion universe and the reason it is included.
/// Composed each run from seed ∪ holdings ∪ watchlist ∪ candidates; departed members are
/// de-activated (never deleted), mirroring the Radar universe pattern (feature 030, FR-002).
/// </summary>
public sealed class AnalystUniverseMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized upper-case ticker.</summary>
    public string Ticker { get; set; } = string.Empty;

    public UniverseReason Reason { get; set; }

    /// <summary>Departed members flip to false; rows are retained for history.</summary>
    public bool Active { get; set; } = true;

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
