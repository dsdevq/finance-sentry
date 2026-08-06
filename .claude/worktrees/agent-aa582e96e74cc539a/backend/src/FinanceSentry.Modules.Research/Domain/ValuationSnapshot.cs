namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// A point-in-time valuation capture for one ticker. Persisted on every computation so the
/// comparison window grows organically (self-built history for metrics with no free historical
/// source). Null metric = unavailable, NEVER zero-filled (feature 030, FR-006).
/// </summary>
public sealed class ValuationSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized upper-case ticker.</summary>
    public string Ticker { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public decimal Price { get; set; }

    public decimal? TrailingPe { get; set; }

    public decimal? ForwardPe { get; set; }

    public decimal? EvToEbitda { get; set; }

    public decimal? DividendYield { get; set; }

    public decimal? ConsensusTarget { get; set; }

    /// <summary>Mirrors quote staleness semantics.</summary>
    public bool IsStale { get; set; }
}
