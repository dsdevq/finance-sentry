namespace FinanceSentry.Modules.Auth.Domain.Entities;

public class McpAuthorizationCode
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public string RedirectUri { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ConsumedAt { get; private set; }

    private McpAuthorizationCode() { }

    public McpAuthorizationCode(string userId, string email, string codeHash, string redirectUri, DateTime expiresAt)
    {
        UserId = userId;
        Email = email;
        CodeHash = codeHash;
        RedirectUri = redirectUri;
        ExpiresAt = expiresAt;
    }

    public bool IsValidFor(string redirectUri)
        => ConsumedAt is null
           && ExpiresAt > DateTime.UtcNow
           && string.Equals(RedirectUri, redirectUri, StringComparison.Ordinal);

    public void Consume() => ConsumedAt = DateTime.UtcNow;
}
