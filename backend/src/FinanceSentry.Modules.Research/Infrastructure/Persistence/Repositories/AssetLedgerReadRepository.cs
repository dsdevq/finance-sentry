namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class AssetLedgerReadRepository(ResearchDbContext db) : IAssetLedgerReadRepository
{
    public async Task<AssetLedgerRead?> GetAsync(Guid userId, string symbol, CancellationToken ct = default)
    {
        var upper = symbol.Trim().ToUpperInvariant();
        return await db.AssetLedgerReads.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Symbol == upper, ct);
    }

    public async Task UpsertAsync(AssetLedgerRead read, CancellationToken ct = default)
    {
        read.Symbol = read.Symbol.Trim().ToUpperInvariant();

        var existing = await db.AssetLedgerReads
            .FirstOrDefaultAsync(r => r.UserId == read.UserId && r.Symbol == read.Symbol, ct);

        if (existing is null)
        {
            db.AssetLedgerReads.Add(read);
        }
        else
        {
            existing.Narrative = read.Narrative;
            existing.SourceFingerprint = read.SourceFingerprint;
            existing.GeneratedAt = read.GeneratedAt;
        }

        await db.SaveChangesAsync(ct);
    }
}
