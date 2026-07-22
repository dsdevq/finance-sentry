namespace FinanceSentry.Modules.Research.Infrastructure.Sources;

/// <summary>
/// Thrown when a scraped <c>Page</c>-kind news source no longer matches its expected structure
/// (missing article list, renamed containers). Surfaces markup drift as a loud, catchable failure
/// (feature 030, FR-009) instead of a silently-empty ingest.
/// </summary>
public sealed class NewsSourceParseException(string message) : Exception(message);
