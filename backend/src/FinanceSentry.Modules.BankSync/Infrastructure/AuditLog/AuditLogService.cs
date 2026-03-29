namespace FinanceSentry.Modules.BankSync.Infrastructure.AuditLog;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Records all data access events to the audit_logs table.
/// Writes are fire-and-forget — never block the request path.
/// Never logs plaintext credentials, tokens, or full account numbers.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Asynchronously records an audit event. Failures are swallowed so they never
    /// impact the main request (graceful degradation).
    /// </summary>
    void Log(
        Guid userId,
        string action,
        string resourceType,
        Guid? resourceId,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IServiceScopeFactory scopeFactory, ILogger<AuditLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Log(
        Guid userId,
        string action,
        string resourceType,
        Guid? resourceId,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        // Fire-and-forget — do not await
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BankSyncDbContext>();

                var entry = AuditLog.Create(userId, action, resourceType, resourceId,
                    ipAddress, userAgent, correlationId);

                db.AuditLogs.Add(entry);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit log failure must NEVER break the main request
                _logger.LogWarning(ex, "Audit log write failed for action {Action} by user {UserId}", action, userId);
            }
        });
    }
}
