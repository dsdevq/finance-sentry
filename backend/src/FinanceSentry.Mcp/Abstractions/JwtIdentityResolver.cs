using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FinanceSentry.Mcp.Abstractions;

/// <summary>
/// Validates the MCP_TOKEN JWT once at startup against the shared Jwt:Secret and caches
/// the resolved userId. The JWT must carry aud=mcp and a Guid sub claim.
/// </summary>
public sealed class JwtIdentityResolver : IIdentityResolver
{
    private const string ExpectedAudience = "mcp";

    private readonly Guid? _userId;
    private readonly string? _email;

    public JwtIdentityResolver(IConfiguration configuration, ILogger<JwtIdentityResolver> logger)
    {
        var token = configuration["Mcp:Token"] ?? Environment.GetEnvironmentVariable("MCP_TOKEN");
        var secret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("MCP_TOKEN is not configured. Tools requiring identity will fail; pass userId explicitly or set MCP_TOKEN.");
            return;
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogError("Jwt:Secret is not configured; cannot validate MCP_TOKEN.");
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = ExpectedAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;

            if (Guid.TryParse(sub, out var parsedUserId))
            {
                _userId = parsedUserId;
                _email = email;
                logger.LogInformation("MCP identity resolved: {Email} ({UserId}).", email ?? "<no-email>", parsedUserId);
            }
            else
            {
                logger.LogError("MCP_TOKEN sub claim is not a valid Guid: {Sub}.", sub);
            }
        }
        catch (SecurityTokenException ex)
        {
            logger.LogError(ex, "MCP_TOKEN failed validation. Regenerate via POST /api/v1/auth/mcp-token.");
        }
    }

    public Guid? GetUserId() => _userId;

    public string? GetEmail() => _email;

    public bool IsConfigured => _userId.HasValue;
}
