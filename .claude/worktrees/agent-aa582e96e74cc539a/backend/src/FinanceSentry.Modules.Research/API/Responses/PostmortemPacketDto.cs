namespace FinanceSentry.Modules.Research.API.Responses;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// FR-008c: the raw material for Denys's periodic process review. The system compiles decision
/// notes, prices, and counterfactuals for a period — it does not judge.
/// </summary>
public record PostmortemPacketDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<PostmortemEntryDto> Entries,
    IReadOnlyList<PostmortemEntryDto> CounterfactualEntries,
    IReadOnlyList<PostmortemOverrideDto> Overrides);

/// <summary>An explicit risk-gate override recorded during the period — reviewed like any decision.</summary>
public record PostmortemOverrideDto(
    DateTimeOffset Timestamp,
    string Scanner,
    string SignalType,
    string Subject,
    IReadOnlyDictionary<string, object> Payload);

public record PostmortemEntryDto(
    Guid SubjectId,
    string Ticker,
    string? DecisionNoteAtCreation,
    string? DecisionNoteAtTerminal,
    decimal? EntryPrice,
    decimal? ExitPrice,
    decimal? ExcessReturnPct,
    ThesisEventType EventTypeTerminal);
