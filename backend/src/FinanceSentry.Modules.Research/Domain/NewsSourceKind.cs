namespace FinanceSentry.Modules.Research.Domain;

/// <summary>How a registered news source is ingested (feature 030).</summary>
public enum NewsSourceKind
{
    /// <summary>An RSS/Atom feed (reuses the existing RSS pipeline).</summary>
    Rss,

    /// <summary>An HTML page scraped for article links (e.g. TrendForce press center).</summary>
    Page,
}
