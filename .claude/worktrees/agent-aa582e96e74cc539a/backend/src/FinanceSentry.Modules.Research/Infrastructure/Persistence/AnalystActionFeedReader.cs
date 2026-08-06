namespace FinanceSentry.Modules.Research.Infrastructure.Persistence;

using FinanceSentry.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implements the companion read contract (feature 031) over analyst actions — rows ingested after a
/// watermark. Keeps the Companion module decoupled from the Research internals.
/// </summary>
public class AnalystActionFeedReader(ResearchDbContext db) : IAnalystActionFeedReader
{
    private const int MaxLimit = 500;

    public async Task<IReadOnlyList<AnalystActionFeedRecord>> GetNewSinceAsync(
        DateTimeOffset watermark, int limit, CancellationToken ct = default)
    {
        var effective = Math.Clamp(limit, 1, MaxLimit);
        return await db.AnalystActions.AsNoTracking()
            .Where(a => a.IngestedAt > watermark)
            .OrderBy(a => a.IngestedAt)
            .Take(effective)
            .Select(a => new AnalystActionFeedRecord(
                a.Id, a.Ticker, a.Firm, a.ActionType.ToString(), a.NewTarget, a.IngestedAt))
            .ToListAsync(ct);
    }
}
