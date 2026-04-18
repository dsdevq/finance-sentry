namespace FinanceSentry.Modules.Auth.Domain.Entities;

public class OAuthState
{
    public Guid Id { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
