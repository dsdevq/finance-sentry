namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR.OAuth;

/// <summary>
/// Configuration for IBKR Web API access over OAuth 1.0a. Unlike the IBeam
/// model there is no per-user container — requests go straight to IBKR's public
/// API host, signed with each user's stored keys.
/// </summary>
public sealed class IbkrOAuthOptions
{
    public const string SectionName = "IbkrOAuth";

    /// <summary>IBKR Web API host. Signed requests and the live-session-token handshake both target this.</summary>
    public string BaseUrl { get; set; } = "https://api.ibkr.com";

    /// <summary>OAuth realm sent in the Authorization header. First-party self-service keys use <c>limited_poa</c>.</summary>
    public string Realm { get; set; } = "limited_poa";
}
