namespace FinanceSentry.Modules.Research.API.Responses;

/// <summary>Result of registering a news source (feature 030, FR-007).</summary>
public record RegisteredSourceDto(Guid SourceId, bool Enabled);

/// <summary>
/// A registered news source with its health fields, so Ledger can see source status directly
/// (feature 030). <see cref="ThesisId"/> null = a market-wide default source.
/// </summary>
public record NewsSourceDto(
    Guid Id,
    string Name,
    string Kind,
    string Url,
    IReadOnlyList<string> Keywords,
    Guid? ThesisId,
    bool Enabled,
    int ConsecutiveFailures,
    DateTimeOffset? LastSuccessAt,
    string? LastFailureReason);
