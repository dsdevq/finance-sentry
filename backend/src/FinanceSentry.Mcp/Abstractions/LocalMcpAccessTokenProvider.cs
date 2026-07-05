using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class LocalMcpAccessTokenProvider(
    LocalMcpCredentialStore credentialStore,
    McpOAuthTokenClient tokenClient,
    IConfiguration configuration,
    ILogger<LocalMcpAccessTokenProvider> logger)
{
    private const string ExpectedAudience = "mcp";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    public ClaimsPrincipal? GetCurrentPrincipal()
    {
        var credentials = credentialStore.Load();
        if (credentials is null)
            return null;

        if (credentials.ExpiresAt <= DateTime.UtcNow.Add(RefreshSkew))
        {
            credentials = TryRefresh(credentials);
            if (credentials is null)
                return null;
        }

        var principal = Validate(credentials.AccessToken);
        if (principal is not null)
            return principal;

        credentials = TryRefresh(credentials);
        return credentials is null ? null : Validate(credentials.AccessToken);
    }

    private StoredMcpCredentials? TryRefresh(StoredMcpCredentials credentials)
    {
        try
        {
            credentials = tokenClient.RefreshAsync(credentials).GetAwaiter().GetResult();
            credentialStore.Save(credentials);
            return credentials;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to refresh stored MCP credentials.");
            return null;
        }
    }

    private ClaimsPrincipal? Validate(string token)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            return null;

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = ExpectedAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Stored MCP access token failed validation.");
            return null;
        }
    }
}
