namespace FinanceSentry.Modules.Radar.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class RadarUniverseRepository(RadarDbContext db) : IRadarUniverseRepository
{
    public async Task<IReadOnlyList<RadarUniverseMember>> ListActiveAsync(CancellationToken ct = default)
        => await db.UniverseMembers.AsNoTracking()
            .Where(m => m.Active)
            .OrderBy(m => m.Ticker)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RadarUniverseMember>> ListAllAsync(CancellationToken ct = default)
        => await db.UniverseMembers.AsNoTracking()
            .OrderBy(m => m.Ticker)
            .ToListAsync(ct);

    public async Task UpsertMembersAsync(
        IReadOnlyCollection<RadarUniverseMember> members, CancellationToken ct = default)
    {
        if (members.Count == 0)
        {
            return;
        }

        var tickers = members.Select(m => m.Ticker).Distinct().ToArray();
        var existing = await db.UniverseMembers
            .Where(m => tickers.Contains(m.Ticker))
            .ToDictionaryAsync(m => m.Ticker, ct);

        foreach (var member in members)
        {
            if (existing.TryGetValue(member.Ticker, out var current))
            {
                current.Active = true;
                current.Kind = member.Kind;
                current.Source = member.Source;
            }
            else
            {
                db.UniverseMembers.Add(member);
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

        await db.UniverseMembers
            .Where(m => tickers.Contains(m.Ticker) && m.Active)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Active, false), ct);
    }
}
