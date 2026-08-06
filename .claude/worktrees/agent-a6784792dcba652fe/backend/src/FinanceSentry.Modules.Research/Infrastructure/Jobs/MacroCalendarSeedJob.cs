namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Modules.Research.Application.Services;
using Microsoft.Extensions.Logging;

public sealed class MacroCalendarSeedJob(
    IMacroCalendarService svc,
    ILogger<MacroCalendarSeedJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var count = await svc.SeedIfEmptyAsync(ct);
        if (count > 0)
        {
            logger.LogInformation("MacroCalendarSeedJob inserted {Count} baseline events", count);
        }
    }
}
