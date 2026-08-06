namespace FinanceSentry.Modules.Risk.Domain.Repositories;

public interface IPolicyViolationAckRepository
{
    Task<IReadOnlyList<PolicyViolationAck>> ListActiveAsync(Guid userId, CancellationToken ct = default);

    Task<PolicyViolationAck?> FindActiveAsync(Guid userId, string ruleKey, string subject, CancellationToken ct = default);

    Task AddAsync(PolicyViolationAck ack, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
