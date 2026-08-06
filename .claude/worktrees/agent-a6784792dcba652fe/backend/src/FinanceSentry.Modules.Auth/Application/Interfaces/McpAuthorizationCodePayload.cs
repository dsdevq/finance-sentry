namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public sealed record McpAuthorizationCodePayload(string UserId, string Email, string RedirectUri);
