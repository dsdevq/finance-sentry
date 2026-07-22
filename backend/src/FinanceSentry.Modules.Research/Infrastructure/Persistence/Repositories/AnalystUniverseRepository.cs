namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class AnalystUniverseRepository(ResearchDbContext db) : IAnalystUniverseRepository
{
    public async Task<IReadOnlyList<AnalystUniverseMember>> ListActiveAsync(CancellationToken ct = default)
        => await db.AnalystUniverseMembers.AsNoTracking().Where(m => m.Active).ToListAsync(ct);

    public async Task<IReadOnlyList<AnalystUniverseMember>> ListAllAsync(CancellationToken ct = default)
        => await db.AnalystUniverseMembers.AsNoTracking().ToListAsync(ct);

    public async Task UpsertMembersAsync(
        IReadOnlyCollection<AnalystUniverseMember> members, CancellationToken ct = default)
    {
        if (members.Count == 0)
        {
            return;
        }

        var tickers = members.Select(m => m.Ticker).ToArray();
        var existing = await db.AnalystUniverseMembers
            .Where(m => tickers.Contains(m.Ticker))
            .ToDictionaryAsync(m => m.Ticker, ct);

        foreach (var member in members)
        {
            if (existing.TryGetValue(member.Ticker, out var row))
            {
                row.Active = true;
                row.Reason = member.Reason;
            }
            else
            {
                db.AnalystUniverseMembers.Add(member);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default)
    {
        if (tickers.Count == 0)
        {
            return;
        }

        await db.AnalystUniverseMembers
            .Where(m => tickers.Contains(m.Ticker) && m.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Active, false), ct);
    }

    public async Task<bool> IsInUniverseAsync(string ticker, CancellationToken ct = default)
    {
        var upper = ticker.Trim().ToUpperInvariant();
        return await db.AnalystUniverseMembers.AsNoTracking()
            .AnyAsync(m => m.Ticker == upper && m.Active, ct);
    }
}
