namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class IpsRepository(ResearchDbContext db) : IIpsRepository
{
    public Task<InvestmentPolicyStatement?> GetCurrentAsync(Guid userId, CancellationToken ct = default)
        => db.PolicyStatements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsCurrent, ct);

    public async Task<IReadOnlyList<InvestmentPolicyStatement>> ListVersionsAsync(Guid userId, CancellationToken ct = default)
        => await db.PolicyStatements.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

    public async Task<int> GetMaxVersionAsync(Guid userId, CancellationToken ct = default)
        => await db.PolicyStatements
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Version)
            .MaxAsync(ct) ?? 0;

    public async Task AddVersionAsync(InvestmentPolicyStatement ips, CancellationToken ct = default)
    {
        // Demote the prior current version so exactly one row is IsCurrent per user.
        await db.PolicyStatements
            .Where(x => x.UserId == ips.UserId && x.IsCurrent)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false), ct);

        db.PolicyStatements.Add(ips);
        await db.SaveChangesAsync(ct);
    }
}
