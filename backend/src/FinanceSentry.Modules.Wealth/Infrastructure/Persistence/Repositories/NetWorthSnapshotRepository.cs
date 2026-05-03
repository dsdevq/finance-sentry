namespace FinanceSentry.Modules.Wealth.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Wealth.Domain;
using FinanceSentry.Modules.Wealth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class NetWorthSnapshotRepository(WealthDbContext db) : INetWorthSnapshotRepository
{
    private readonly WealthDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task PersistAsync(NetWorthSnapshot snapshot, CancellationToken ct = default)
    {
        _db.NetWorthSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid userId, DateOnly snapshotDate, CancellationToken ct = default)
        => _db.NetWorthSnapshots.AnyAsync(s => s.UserId == userId && s.SnapshotDate == snapshotDate, ct);

    public async Task<IReadOnlyList<NetWorthSnapshot>> GetByUserIdAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var query = _db.NetWorthSnapshots.Where(s => s.UserId == userId);

        if (from.HasValue)
            query = query.Where(s => s.SnapshotDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.SnapshotDate <= to.Value);

        return await query.OrderBy(s => s.SnapshotDate).ToListAsync(ct);
    }
}
