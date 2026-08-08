namespace FinanceSentry.Modules.Agent.Application.Services;

/// <summary>The brain backing the interactive agent — resolved from <see cref="AgentOptions"/> at startup.</summary>
public enum AgentProvider
{
    /// <summary>No credentials — the endpoint returns <c>agent_not_configured</c> and never calls out.</summary>
    None,

    /// <summary>FS-native loop: composed persona + bridged MCP tools over the Anthropic Messages API.</summary>
    Anthropic,

    /// <summary>Delegates the whole turn to the OpenClaw Ledger (<c>finance</c>) agent — its persona/tools/creds.</summary>
    OpenClaw,
}

/// <summary>
/// Bound from the <c>Agent</c> configuration section. Two interchangeable brains are supported (feature
/// 040): the FS-native Anthropic loop (<c>Agent__Anthropic__ApiKey</c>, server-only) and delegation to
/// the existing OpenClaw Ledger agent (<c>Agent__OpenClaw__*</c>). The active one is resolved by
/// <see cref="Provider"/>. With neither configured the agent is "not configured": the endpoint returns a
/// clear error and never calls the model — mirroring the Finnhub/FRED keyless-silent precedent.
/// </summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public AnthropicOptions Anthropic { get; set; } = new();

    public OpenClawOptions OpenClaw { get; set; } = new();

    /// <summary>
    /// Optional explicit brain selection (<c>anthropic</c>|<c>openclaw</c>). Unset = auto: an Anthropic
    /// key wins (FS-native), otherwise a configured OpenClaw gateway. Lets both coexist and be flipped
    /// without touching credentials.
    /// </summary>
    public string? ProviderOverride { get; set; }

    /// <summary>The resolved brain for this process (see <see cref="ProviderOverride"/>).</summary>
    public AgentProvider Provider
    {
        get
        {
            if (string.Equals(ProviderOverride, "openclaw", StringComparison.OrdinalIgnoreCase))
            {
                return OpenClaw.IsConfigured ? AgentProvider.OpenClaw : AgentProvider.None;
            }

            if (string.Equals(ProviderOverride, "anthropic", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(Anthropic.ApiKey) ? AgentProvider.None : AgentProvider.Anthropic;
            }

            if (!string.IsNullOrWhiteSpace(Anthropic.ApiKey))
            {
                return AgentProvider.Anthropic;
            }

            return OpenClaw.IsConfigured ? AgentProvider.OpenClaw : AgentProvider.None;
        }
    }

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

    /// <summary>True once a usable brain (Anthropic key or OpenClaw gateway) is configured.</summary>
    public bool IsConfigured => Provider != AgentProvider.None;

    public sealed class AnthropicOptions
    {
        public string? ApiKey { get; set; }

        public string BaseUrl { get; set; } = "https://api.anthropic.com";

        public string ApiVersion { get; set; } = "2023-06-01";
    }

    /// <summary>
    /// Delegation to the OpenClaw gateway's OpenAI-compatible API (<c>POST /v1/chat/completions</c>). The
    /// browser chat routes to the Ledger <c>finance</c> agent, which owns persona/tools/credentials — no
    /// Anthropic key needed in FS. Empty <see cref="BaseUrl"/> = this brain is off.
    /// </summary>
    public sealed class OpenClawOptions
    {
        /// <summary>Gateway base URL, e.g. <c>http://172.17.0.1:18789</c> (bind: lan). Empty = disabled.</summary>
        public string? BaseUrl { get; set; }

        /// <summary>Gateway bearer token (<c>OPENCLAW_GATEWAY_TOKEN</c>). Omit only if gateway auth is <c>none</c>.</summary>
        public string? Token { get; set; }

        /// <summary>OpenAI <c>model</c> selector routing to the Ledger agent.</summary>
        public string Model { get; set; } = "openclaw/finance";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
    }
}
