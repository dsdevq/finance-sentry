namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class MacroCalendarService(
    IMacroCalendarRepository repo,
    ResearchDbContext db,
    ILogger<MacroCalendarService> logger) : IMacroCalendarService
{
    public Task<IReadOnlyList<MacroEvent>> QueryAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<string>? regions,
        string? minImportance,
        CancellationToken ct = default)
        => repo.QueryAsync(from, to, regions, minImportance, ct);

    public async Task<int> SeedIfEmptyAsync(CancellationToken ct = default)
    {
        var any = await db.MacroEvents.AnyAsync(ct);
        if (any)
        {
            return 0;
        }

        var inserted = await repo.UpsertAsync(BuildSeed(), ct);
        logger.LogInformation("MacroCalendarService seeded {Count} 2026 events", inserted);
        return inserted;
    }

    private static List<MacroEvent> BuildSeed()
    {
        static MacroEvent Ev(int y, int m, int d, string ev, string region, string importance, string src, int? hour = null, int? minute = null)
            => new()
            {
                EventDate = new DateOnly(y, m, d),
                EventTime = hour is int h && minute is int mm ? new TimeOnly(h, mm) : null,
                Event = ev,
                Region = region,
                Importance = importance,
                Source = src,
            };

        return
        [
            Ev(2026, 7, 30, "FOMC rate decision", "US", MacroImportance.High, "https://www.federalreserve.gov/monetarypolicy/fomccalendars.htm", 14, 0),
            Ev(2026, 9, 17, "FOMC rate decision + SEP", "US", MacroImportance.High, "https://www.federalreserve.gov/monetarypolicy/fomccalendars.htm", 14, 0),
            Ev(2026, 10, 29, "FOMC rate decision", "US", MacroImportance.High, "https://www.federalreserve.gov/monetarypolicy/fomccalendars.htm", 14, 0),
            Ev(2026, 12, 10, "FOMC rate decision + SEP", "US", MacroImportance.High, "https://www.federalreserve.gov/monetarypolicy/fomccalendars.htm", 14, 0),

            Ev(2026, 7, 15, "CPI (Jun)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/cpi.htm", 8, 30),
            Ev(2026, 8, 12, "CPI (Jul)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/cpi.htm", 8, 30),
            Ev(2026, 9, 11, "CPI (Aug)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/cpi.htm", 8, 30),
            Ev(2026, 10, 15, "CPI (Sep)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/cpi.htm", 8, 30),
            Ev(2026, 11, 13, "CPI (Oct)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/cpi.htm", 8, 30),
            Ev(2026, 12, 10, "CPI (Nov)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/cpi.htm", 8, 30),

            Ev(2026, 7, 2, "Nonfarm payrolls (Jun)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/empsit.htm", 8, 30),
            Ev(2026, 8, 6, "Nonfarm payrolls (Jul)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/empsit.htm", 8, 30),
            Ev(2026, 9, 3, "Nonfarm payrolls (Aug)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/empsit.htm", 8, 30),
            Ev(2026, 10, 1, "Nonfarm payrolls (Sep)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/empsit.htm", 8, 30),
            Ev(2026, 11, 5, "Nonfarm payrolls (Oct)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/empsit.htm", 8, 30),
            Ev(2026, 12, 3, "Nonfarm payrolls (Nov)", "US", MacroImportance.High, "https://www.bls.gov/schedule/news_release/empsit.htm", 8, 30),

            Ev(2026, 7, 23, "ECB rate decision", "EU", MacroImportance.High, "https://www.ecb.europa.eu/press/calendars/mgcgc/html/index.en.html", 13, 15),
            Ev(2026, 9, 10, "ECB rate decision + projections", "EU", MacroImportance.High, "https://www.ecb.europa.eu/press/calendars/mgcgc/html/index.en.html", 13, 15),
            Ev(2026, 10, 29, "ECB rate decision", "EU", MacroImportance.High, "https://www.ecb.europa.eu/press/calendars/mgcgc/html/index.en.html", 13, 15),
            Ev(2026, 12, 17, "ECB rate decision + projections", "EU", MacroImportance.High, "https://www.ecb.europa.eu/press/calendars/mgcgc/html/index.en.html", 13, 15),
        ];
    }
}
