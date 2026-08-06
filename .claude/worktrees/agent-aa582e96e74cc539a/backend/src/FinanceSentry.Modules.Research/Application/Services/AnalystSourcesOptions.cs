namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Configuration for analyst-signal sources (feature 037). Scraped sources carry an
/// <c>Enabled</c> demotion flag (FR-004 — demoting a scraper is a config flip, not a code
/// change); structured sources additionally require an API key and stay silent without one
/// (FR-002).
/// </summary>
public sealed class AnalystSourcesOptions
{
    public const string SectionName = "AnalystSources";

    public MarketbeatOptions Marketbeat { get; set; } = new();

    public FinnhubOptions Finnhub { get; set; } = new();

    public sealed class MarketbeatOptions
    {
        public bool Enabled { get; set; } = true;
    }

    public sealed class FinnhubOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>From <c>FINNHUB_API_KEY</c>; empty means the trends capture is skipped (FR-002).</summary>
        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";

        /// <summary>Pacing target; Finnhub's free cap is 60/min plus a 30/sec global cap (FR-005).</summary>
        public int RequestsPerMinute { get; set; } = 50;
    }
}
