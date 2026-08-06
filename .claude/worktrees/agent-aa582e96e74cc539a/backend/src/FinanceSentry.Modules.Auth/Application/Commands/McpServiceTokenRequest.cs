namespace FinanceSentry.Modules.Auth.Application.Commands;

public sealed record McpServiceTokenRequest(string? Label = null, int? LifetimeDays = null);

public sealed record McpServiceTokenRevokeRequest(Guid Jti);
