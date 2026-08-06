using System.Text.Json.Serialization;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public sealed record McpTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType,
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("redirect_uri")] string? RedirectUri = null,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);
