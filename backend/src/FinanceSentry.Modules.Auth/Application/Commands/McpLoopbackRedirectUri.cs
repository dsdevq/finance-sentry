using FinanceSentry.Modules.Auth.Domain.Exceptions;

namespace FinanceSentry.Modules.Auth.Application.Commands;

internal static class McpLoopbackRedirectUri
{
    public static string Validate(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            throw new InvalidRefreshTokenException("Invalid MCP redirect URI.");

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            throw new InvalidRefreshTokenException("MCP redirect URI must use http.");

        if (!uri.IsLoopback)
            throw new InvalidRefreshTokenException("MCP redirect URI must target a loopback host.");

        if (!string.IsNullOrWhiteSpace(uri.Fragment))
            throw new InvalidRefreshTokenException("MCP redirect URI must not include a fragment.");

        return uri.ToString();
    }
}
