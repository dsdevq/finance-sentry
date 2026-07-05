using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Mcp.Abstractions;

public sealed class McpOAuthTokenClient(ILogger<McpOAuthTokenClient> logger)
{
    public async Task<StoredMcpCredentials> ExchangeAuthorizationCodeAsync(
        string apiBaseUrl,
        string frontendBaseUrl,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient();
        var response = await http.PostAsJsonAsync(
            $"{apiBaseUrl.TrimEnd('/')}/auth/mcp/token",
            new { grant_type = "authorization_code", code, redirect_uri = redirectUri },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<McpTokenPayload>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MCP token response was empty.");

        return payload.ToStored(apiBaseUrl, frontendBaseUrl);
    }

    public async Task<StoredMcpCredentials> RefreshAsync(
        StoredMcpCredentials current,
        CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient();
        var response = await http.PostAsJsonAsync(
            $"{current.ApiBaseUrl.TrimEnd('/')}/auth/mcp/token",
            new { grant_type = "refresh_token", refresh_token = current.RefreshToken },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<McpTokenPayload>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MCP refresh response was empty.");

        return payload.ToStored(current.ApiBaseUrl, current.FrontendBaseUrl);
    }

    public async Task RevokeAsync(StoredMcpCredentials current, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient();
            var response = await http.PostAsJsonAsync(
                $"{current.ApiBaseUrl.TrimEnd('/')}/auth/mcp/revoke",
                new { refresh_token = current.RefreshToken },
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to revoke MCP refresh token cleanly.");
        }
    }

    private sealed record McpTokenPayload(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        DateTime ExpiresAt,
        string UserId,
        string Email,
        string Scope)
    {
        public StoredMcpCredentials ToStored(string apiBaseUrl, string frontendBaseUrl)
            => new(
                apiBaseUrl,
                frontendBaseUrl,
                AccessToken,
                RefreshToken,
                ExpiresAt,
                UserId,
                Email,
                Scope);
    }
}
