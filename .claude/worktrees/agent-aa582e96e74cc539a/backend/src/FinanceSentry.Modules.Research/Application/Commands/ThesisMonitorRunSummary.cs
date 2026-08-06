namespace FinanceSentry.Modules.Research.Application.Commands;

/// <summary>
/// Aggregate outcome of one thesis-monitor run (FR-016) — returned in-band from
/// <see cref="RunThesisMonitorCommand"/> and logged by <c>ThesisMonitorJob</c>.
/// </summary>
public record ThesisMonitorRunSummary(
    int ThesesEvaluated,
    int TriggersEvaluated,
    int BreaksRaised,
    int BreaksCleared,
    int Skipped,
    int Errors);
