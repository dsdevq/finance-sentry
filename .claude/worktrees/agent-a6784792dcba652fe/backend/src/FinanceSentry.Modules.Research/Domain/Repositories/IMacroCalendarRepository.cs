namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface IMacroCalendarRepository
{
    Task<IReadOnlyList<MacroEvent>> QueryAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<string>? regions,
        string? minImportance,
        CancellationToken ct = default);

    Task<int> UpsertAsync(IReadOnlyCollection<MacroEvent> events, CancellationToken ct = default);
}
