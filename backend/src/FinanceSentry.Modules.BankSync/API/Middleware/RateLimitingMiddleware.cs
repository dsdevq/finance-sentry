namespace FinanceSentry.Modules.BankSync.API.Middleware;

/// <summary>
/// Rate limiting policy name constants used with ASP.NET Core built-in rate limiter.
/// Actual policies are registered in Program.cs via AddRateLimiter.
/// </summary>
public static class RateLimitingPolicies
{
    /// <summary>100 requests/min for authenticated users (per user ID).</summary>
    public const string Authenticated = "authenticated";

    /// <summary>10 requests/min for anonymous users (per IP).</summary>
    public const string Anonymous = "anonymous";

    /// <summary>Exempt from all rate limiting (Plaid webhooks, health checks).</summary>
    public const string Exempt = "exempt";
}
