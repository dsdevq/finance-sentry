namespace FinanceSentry.Modules.Auth.Application.Interfaces;

public interface IMcpAuthorizationCodeStore
{
    Task<string> IssueAsync(string userId, string email, string redirectUri, CancellationToken cancellationToken = default);
    Task<McpAuthorizationCodePayload?> ConsumeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
}
