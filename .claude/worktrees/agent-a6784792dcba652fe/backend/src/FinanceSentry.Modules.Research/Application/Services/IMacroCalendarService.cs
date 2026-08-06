namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IMacroCalendarService
{
    Task<IReadOnlyList<MacroEvent>> QueryAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<string>? regions,
        string? minImportance,
        CancellationToken ct = default);

    Task<int> SeedIfEmptyAsync(CancellationToken ct = default);
}
