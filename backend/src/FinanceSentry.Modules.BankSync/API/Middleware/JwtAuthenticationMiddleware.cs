namespace FinanceSentry.Modules.BankSync.API.Middleware;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Middleware that validates Bearer JWT tokens on every request.
/// Exempt paths: /health, /swagger, /api/webhook/plaid, /hangfire.
/// Attaches ClaimsPrincipal (including user ID) to HttpContext.User on success.
/// Returns 401 for missing/invalid/expired tokens on protected paths.
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly TokenValidationParameters _validationParams;

    private static readonly string[] _exemptPrefixes =
    [
        "/health",
        "/swagger",
        "/api/webhook",
        "/hangfire"
    ];

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        _validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParams, out _);
            context.User = principal;
            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Expired JWT token received.");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Token has expired. Please sign in again.", errorCode = "TOKEN_EXPIRED" });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Invalid JWT token: {Message}", ex.Message);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid authentication token.", errorCode = "TOKEN_INVALID" });
        }
    }

    private static bool IsExempt(string path)
    {
        foreach (var prefix in _exemptPrefixes)
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
