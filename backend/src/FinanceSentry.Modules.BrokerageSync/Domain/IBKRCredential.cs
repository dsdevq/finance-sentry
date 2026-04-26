namespace FinanceSentry.Modules.BrokerageSync.Domain;

/// <summary>
/// Represents a single user's link to an IBKR account.
///
/// Single-tenant model: the IBKR Client Portal Gateway (run as the `ibkr-gateway`
/// sidecar via IBeam) owns the IBKR session for the entire deployment using
/// credentials supplied via environment variables. This entity therefore stores
/// only the discovered IBKR <see cref="AccountId"/> and the link metadata — no
/// per-user IBKR credentials.
///
/// When this app is opened to multiple end users we will move to IBKR's OAuth
/// Web API (each user authorises Finance Sentry against their own IBKR account)
/// and add encrypted access/refresh-token columns at that point.
/// </summary>
public sealed class IBKRCredential
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? AccountId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncError { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private IBKRCredential() { }

    public IBKRCredential(Guid userId, string accountId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AccountId = accountId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAccountId(string accountId) => AccountId = accountId;

    public void RecordSyncSuccess()
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncError = null;
    }

    public void RecordSyncError(string error) => LastSyncError = error;

    public void Deactivate() => IsActive = false;
}
