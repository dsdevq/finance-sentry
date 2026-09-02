namespace FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="ICounterpartyRepository"/>.
/// </summary>
public class CounterpartyRepository(BankSyncDbContext context) : ICounterpartyRepository
{
    private readonly BankSyncDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<IReadOnlyList<Counterparty>> GetForUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Counterparties
            .Include(c => c.Rules)
            .Where(c => c.UserId == userId || c.UserId == Guid.Empty)
            .ToListAsync(cancellationToken);
    }
}
