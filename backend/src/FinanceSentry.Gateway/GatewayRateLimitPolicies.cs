namespace FinanceSentry.Gateway;

/// <summary>
/// Named rate-limiter policy identifiers referenced by YARP routes (via each route's
/// <c>RateLimiterPolicy</c> in <c>appsettings.json</c>) and registered on the ASP.NET Core
/// <c>RateLimiter</c> in <c>Program.cs</c>. Keeping them as constants avoids magic strings drifting
/// between the config and the middleware registration (feature 025, FR-004).
/// </summary>
public static class GatewayRateLimitPolicies
{
    /// <summary>Per-client limit on unauthenticated auth endpoints (login, register, refresh, google).</summary>
    public const string Auth = "auth";
}
