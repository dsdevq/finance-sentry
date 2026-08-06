namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// The single hook point every thesis/candidate lifecycle write path calls to append a price-stamped
/// <see cref="ThesisEvent"/> (R1). Never blocks or throws on a quote failure — appends a pending event
/// instead (FR-003).
/// </summary>
public interface IThesisEventRecorder
{
    Task RecordAsync(
        Guid userId,
        ThesisSubjectType subjectType,
        Guid subjectId,
        string ticker,
        ThesisEventType eventType,
        string? decisionNote = null,
        CancellationToken ct = default);
}
