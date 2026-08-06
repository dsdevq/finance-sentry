namespace FinanceSentry.Modules.Risk.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Risk.Domain;
using FinanceSentry.Modules.Risk.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class HoldingSnapshotRepository(RiskDbContext db) : IHoldingSnapshotRepository
{
    public async Task AddRangeAsync(IReadOnlyList<HoldingSnapshot> snapshots, CancellationToken ct = default)
    {
        if (snapshots.Count == 0)
        {
            return;
        }

        db.HoldingSnapshots.AddRange(snapshots);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<HoldingSnapshot>> ListSinceAsync(
        Guid userId, DateTimeOffset since, CancellationToken ct = default)
        => await db.HoldingSnapshots
            .Where(s => s.UserId == userId && s.CapturedAt >= since)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HoldingSnapshot>> ListForSymbolAsync(
        Guid userId, string symbol, string sleeve, CancellationToken ct = default)
        => await db.HoldingSnapshots
            .Where(s => s.UserId == userId && s.Symbol == symbol && s.Sleeve == sleeve)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetUserIdsAsync(CancellationToken ct = default)
        => await db.HoldingSnapshots
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);
}
