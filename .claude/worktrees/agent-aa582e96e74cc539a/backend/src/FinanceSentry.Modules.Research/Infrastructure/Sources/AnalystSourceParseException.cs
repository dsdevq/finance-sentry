namespace FinanceSentry.Modules.Research.Infrastructure.Sources;

/// <summary>
/// Thrown when a scraped analyst-actions source no longer matches its expected structure (missing
/// table, renamed columns). Surfaces markup drift as a loud, catchable failure (FR-009) instead of
/// a silently-empty result.
/// </summary>
public sealed class AnalystSourceParseException(string message) : Exception(message);
