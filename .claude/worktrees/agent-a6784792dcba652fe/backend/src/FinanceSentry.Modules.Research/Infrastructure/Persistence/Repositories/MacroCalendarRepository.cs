namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class MacroCalendarRepository(ResearchDbContext db) : IMacroCalendarRepository
{
    public async Task<IReadOnlyList<MacroEvent>> QueryAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<string>? regions,
        string? minImportance,
        CancellationToken ct = default)
    {
        var q = db.MacroEvents.AsNoTracking()
            .Where(e => e.EventDate >= from && e.EventDate <= to);

        if (regions is { Count: > 0 })
        {
            var upper = regions.Select(r => r.ToUpperInvariant()).ToArray();
            q = q.Where(e => upper.Contains(e.Region));
        }

        if (!string.IsNullOrEmpty(minImportance))
        {
            var rank = ImportanceRank(minImportance);
            q = q.Where(e =>
                (e.Importance == MacroImportance.High ? 3 :
                    e.Importance == MacroImportance.Medium ? 2 : 1) >= rank);
        }

        return await q.OrderBy(e => e.EventDate).ThenBy(e => e.EventTime).ToListAsync(ct);
    }

    public async Task<int> UpsertAsync(IReadOnlyCollection<MacroEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0)
        {
            return 0;
        }

        var dates = events.Select(e => e.EventDate).Distinct().ToArray();
        var eventNames = events.Select(e => e.Event).Distinct().ToArray();

        var candidates = await db.MacroEvents
            .Where(x => dates.Contains(x.EventDate) && eventNames.Contains(x.Event))
            .ToListAsync(ct);
        var existingMap = candidates.ToDictionary(x => (x.EventDate, x.Region, x.Event));

        var inserted = 0;
        foreach (var ev in events)
        {
            if (existingMap.TryGetValue((ev.EventDate, ev.Region, ev.Event), out var row))
            {
                row.EventTime = ev.EventTime;
                row.Importance = ev.Importance;
                row.Source = ev.Source;
            }
            else
            {
                db.MacroEvents.Add(ev);
                inserted++;
            }
        }

        await db.SaveChangesAsync(ct);
        return inserted;
    }

    private static int ImportanceRank(string importance) => importance.ToLowerInvariant() switch
    {
        "high" => 3,
        "medium" => 2,
        _ => 1,
    };
}
