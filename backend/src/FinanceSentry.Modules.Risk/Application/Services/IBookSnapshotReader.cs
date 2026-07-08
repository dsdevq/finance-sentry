namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Modules.Risk.Domain;

/// <summary>Aggregates the three Core holdings/account readers into one <see cref="BookSnapshot"/>.</summary>
public interface IBookSnapshotReader
{
    Task<BookSnapshot> ReadAsync(Guid userId, CancellationToken ct = default);
}
