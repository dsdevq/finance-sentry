namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// A registered external news source. When <see cref="ThesisId"/> is set the source is attached to a
/// specific thesis (e.g. TrendForce → DRAM); when null it is a market-wide default feed. Carries its
/// own per-source failure state so ingestion failures surface via the freshness-alert path after two
/// consecutive failures (feature 030, FR-007/FR-009).
/// </summary>
public sealed class NewsSource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name, e.g. "TrendForce Press Center".</summary>
    public string Name { get; set; } = string.Empty;

    public NewsSourceKind Kind { get; set; }

    /// <summary>Feed or page URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Optional keyword filters for tagging/inclusion; empty = no keyword filter.</summary>
    public List<string> Keywords { get; set; } = [];

    /// <summary>Thesis this source is registered to; null = market-wide default source.</summary>
    public Guid? ThesisId { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Consecutive ingestion failures; reset on success, alert at ≥ 2 (FR-009).</summary>
    public int ConsecutiveFailures { get; set; }

    public DateTimeOffset? LastSuccessAt { get; set; }

    public string? LastFailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
