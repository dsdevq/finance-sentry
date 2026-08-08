namespace FinanceSentry.Modules.Agent.Application.Services;

/// <summary>
/// Bound from the <c>Agent</c> configuration section. The Anthropic API key comes from
/// <c>Agent__Anthropic__ApiKey</c> (server-only, never client-exposed). When the key is unset the
/// agent is "not configured": the endpoint returns a clear error and never calls the model — mirroring
/// the Finnhub/FRED keyless-silent precedent.
/// </summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public AnthropicOptions Anthropic { get; set; } = new();

    /// <summary>Default Claude model for the interactive agent. Opus is a config-selectable escalation.</summary>
    public string ModelId { get; set; } = "claude-sonnet-5";

    /// <summary>Max tokens the model may emit per turn.</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>Upper bound on call→tool→call iterations, guarding runaway loops.</summary>
    public int MaxToolIterations { get; set; } = 12;

    /// <summary>Most-recent conversation turns replayed to the model per request (context budget).</summary>
    public int HistoryTurnBudget { get; set; } = 40;

    /// <summary>
    /// Optional explicit path to the repo root holding <c>agent/ledger/*.md</c>. Usually left unset —
    /// the composer resolves the files from the publish output / repo checkout automatically.
    /// </summary>
    public string? PersonaRootPath { get; set; }

    /// <summary>
    /// Optional allow-list of MCP tool names exposed to the browser runtime. Empty = expose all
    /// (money/trade/credential tools are absent by construction, so the default surface is tier-3-safe).
    /// </summary>
    public IReadOnlyList<string> ToolAllowList { get; set; } = [];

    /// <summary>True once an Anthropic API key is present.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Anthropic.ApiKey);

    public sealed class AnthropicOptions
    {
        public string? ApiKey { get; set; }

        public string BaseUrl { get; set; } = "https://api.anthropic.com";

        public string ApiVersion { get; set; } = "2023-06-01";
    }
}
