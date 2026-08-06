namespace FinanceSentry.Modules.Analytics.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Analytics.Domain;
using FinanceSentry.Modules.Analytics.Domain.Repositories;

/// <summary>Writes audit rows on the app's normal (writable) connection — never <c>fs_readonly</c>.</summary>
public class QueryAuditRepository(AnalyticsDbContext db) : IQueryAuditRepository
{
    public async Task AppendAsync(QueryAuditRecord record, CancellationToken ct = default)
    {
        db.QueryAudit.Add(record);
        await db.SaveChangesAsync(ct);
    }
}
