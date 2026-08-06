namespace FinanceSentry.Modules.Companion.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class CompanionCaptureStateRepository(CompanionDbContext db) : ICompanionCaptureStateRepository
{
    public async Task<DateTimeOffset> GetWatermarkAsync(
        string source, DateTimeOffset floor, CancellationToken ct = default)
    {
        var row = await db.CaptureState.AsNoTracking().FirstOrDefaultAsync(s => s.Source == source, ct);
        return row?.Watermark ?? floor;
    }

    public async Task SetWatermarkAsync(string source, DateTimeOffset watermark, CancellationToken ct = default)
    {
        var row = await db.CaptureState.FirstOrDefaultAsync(s => s.Source == source, ct);
        if (row is null)
        {
            db.CaptureState.Add(new CompanionCaptureState { Source = source, Watermark = watermark });
        }
        else
        {
            row.Watermark = watermark;
        }

        await db.SaveChangesAsync(ct);
    }
}
