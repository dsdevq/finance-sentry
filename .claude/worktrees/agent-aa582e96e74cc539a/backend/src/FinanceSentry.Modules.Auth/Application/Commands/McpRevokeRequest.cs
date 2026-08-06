using System.Text.Json.Serialization;

namespace FinanceSentry.Modules.Auth.Application.Commands;

public sealed record McpRevokeRequest(
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);
