namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// One street event about one ticker (upgrade / downgrade / initiate / target change / reiterate /
/// top-idea). Global market data — no <c>UserId</c> (precedent: <see cref="NewsArticle"/>,
/// <see cref="QuoteCacheEntry"/>). Deduplicated across sources by logical identity
/// (Ticker + Firm + ActionDate + ActionType) — FR-003.
/// </summary>
public sealed class AnalystAction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized upper-case ticker.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Research firm name, normalized (trimmed, canonical casing).</summary>
    public string Firm { get; set; } = string.Empty;

    public AnalystActionType ActionType { get; set; }

    public string? PriorRating { get; set; }

    public string? NewRating { get; set; }

    public decimal? PriorTarget { get; set; }

    public decimal? NewTarget { get; set; }

    /// <summary>The street event date (not the retrieval date).</summary>
    public DateOnly ActionDate { get; set; }

    /// <summary>Originating source reference — <c>marketbeat</c> | <c>yahoo</c>.</summary>
    public string Source { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
}
