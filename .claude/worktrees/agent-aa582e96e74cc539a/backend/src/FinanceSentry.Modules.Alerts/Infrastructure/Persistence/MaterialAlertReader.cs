namespace FinanceSentry.Modules.Alerts.Infrastructure.Persistence;

using FinanceSentry.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implements the companion read contract (feature 031) over the alerts table — active alerts created
/// after a watermark. Keeps the Companion module decoupled from the Alerts internals.
/// </summary>
public class MaterialAlertReader(AlertsDbContext db) : IMaterialAlertReader
{
    private const int MaxLimit = 500;

    public async Task<IReadOnlyList<MaterialAlertRecord>> GetNewSinceAsync(
        DateTimeOffset watermark, int limit, CancellationToken ct = default)
    {
        var effective = Math.Clamp(limit, 1, MaxLimit);
        return await db.Alerts.AsNoTracking()
            .Where(a => a.CreatedAt > watermark && !a.IsDismissed)
            .OrderBy(a => a.CreatedAt)
            .Take(effective)
            .Select(a => new MaterialAlertRecord(
                a.Id, a.UserId, a.Type, a.Severity, a.Title, a.ReferenceId, a.ReferenceLabel, a.CreatedAt))
            .ToListAsync(ct);
    }
}
