using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class LocalMcpSession(
    LocalMcpCredentialStore credentialStore,
    McpOAuthTokenClient tokenClient,
    IConfiguration configuration,
    ILogger<LocalMcpSession> logger)
{
    private const string ExpectedAudience = "mcp";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private readonly object _sync = new();
    private ClaimsPrincipal? _cachedPrincipal;
    private DateTime _cachedExpiresAt;

    public ClaimsPrincipal? GetPrincipal()
    {
        lock (_sync)
        {
            return _cachedExpiresAt > DateTime.UtcNow.Add(RefreshSkew)
                ? _cachedPrincipal
                : null;
        }
    }

    public void Invalidate()
    {
        lock (_sync)
        {
            _cachedPrincipal = null;
            _cachedExpiresAt = DateTime.MinValue;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RefreshIfNeededAsync(forceValidation: true, cancellationToken);
    }

    public async Task RefreshIfNeededAsync(bool forceValidation = false, CancellationToken cancellationToken = default)
    {
        var credentials = credentialStore.Load();
        if (credentials is null)
        {
            Invalidate();
            return;
        }

        if (credentials.ExpiresAt <= DateTime.UtcNow.Add(RefreshSkew))
        {
            credentials = await TryRefreshAsync(credentials, cancellationToken);
            if (credentials is null)
            {
                Invalidate();
                return;
            }
        }

        if (!forceValidation && TryUseCached(credentials.ExpiresAt))
            return;

        var principal = Validate(credentials.AccessToken);
        if (principal is not null)
        {
            Cache(principal, credentials.ExpiresAt);
            return;
        }

        credentials = await TryRefreshAsync(credentials, cancellationToken);
        if (credentials is null)
        {
            Invalidate();
            return;
        }

        principal = Validate(credentials.AccessToken);
        if (principal is null)
        {
            Invalidate();
            return;
        }

        Cache(principal, credentials.ExpiresAt);
    }

    private bool TryUseCached(DateTime expiresAt)
    {
        lock (_sync)
        {
            return _cachedPrincipal is not null && _cachedExpiresAt == expiresAt;
        }
    }

    private void Cache(ClaimsPrincipal principal, DateTime expiresAt)
    {
        lock (_sync)
        {
            _cachedPrincipal = principal;
            _cachedExpiresAt = expiresAt;
        }
    }

    private async Task<StoredMcpCredentials?> TryRefreshAsync(
        StoredMcpCredentials credentials,
        CancellationToken cancellationToken)
    {
        try
        {
            var refreshed = await tokenClient.RefreshAsync(credentials, cancellationToken);
            credentialStore.Save(refreshed);
            return refreshed;
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
