namespace FinanceSentry.Modules.Radar.Infrastructure.Persistence.Repositories;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class RadarSignalRepository(RadarDbContext db) : IRadarSignalRepository
{
    public async Task AppendAsync(RadarSignal signal, CancellationToken ct = default)
    {
        db.RadarSignals.Add(signal);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> HasRecentAsync(string dedupKey, DateTimeOffset since, CancellationToken ct = default)
        => db.RadarSignals.AsNoTracking()
            .AnyAsync(s => s.DedupKey == dedupKey && s.Timestamp >= since, ct);

    public async Task<IReadOnlyList<RadarSignal>> ListAsync(SignalFilter filter, CancellationToken ct = default)
    {
        var query = db.RadarSignals.AsNoTracking().AsQueryable();

        if (filter.Since is not null)
        {
            query = query.Where(s => s.Timestamp >= filter.Since.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Scanner))
        {
            query = query.Where(s => s.Scanner == filter.Scanner);
        }

        if (!string.IsNullOrWhiteSpace(filter.SignalType))
        {
            query = query.Where(s => s.SignalType == filter.SignalType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Subject))
        {
            query = query.Where(s => s.Subject == filter.Subject);
        }

        if (filter.UserId is not null)
        {
            // Global signals (breadth, rotation, unusual moves on non-held tickers) carry no
            // UserId; a user-scoped read must still see them or the market disappears.
            query = query.Where(s => s.UserId == null || s.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Severity) &&
            Enum.TryParse<SignalSeverity>(filter.Severity, ignoreCase: true, out var severity))
        {
            query = query.Where(s => s.Severity == severity);
        }

        return await query.OrderByDescending(s => s.Timestamp).ToListAsync(ct);
    }

    public async Task<int> PruneInfoBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default)
        => await db.RadarSignals
            .Where(s => s.Severity == SignalSeverity.Info && s.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);
}
