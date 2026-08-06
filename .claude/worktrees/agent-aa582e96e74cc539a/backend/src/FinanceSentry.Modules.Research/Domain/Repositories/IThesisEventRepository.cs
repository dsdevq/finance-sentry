namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface IThesisEventRepository
{
    Task AppendAsync(ThesisEvent thesisEvent, CancellationToken ct = default);

    Task<IReadOnlyList<ThesisEvent>> ListAsync(
        Guid userId, Guid? subjectId = null, CancellationToken ct = default);

    Task<IReadOnlyList<ThesisEvent>> ListPendingAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ThesisEvent>> ListForPeriodAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<ThesisEvent?> GetLatestForSubjectAsync(
        ThesisSubjectType subjectType, Guid subjectId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetUserIdsWithEventsAsync(CancellationToken ct = default);

    Task UpdatePricesAsync(ThesisEvent thesisEvent, CancellationToken ct = default);
}
