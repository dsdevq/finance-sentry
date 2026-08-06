namespace FinanceSentry.Modules.Budgets.Domain.Repositories;

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Budget?> FindByUserAndCategoryAsync(Guid userId, string category, CancellationToken ct = default);
    Task<Budget> CreateAsync(Budget budget, CancellationToken ct = default);
    Task UpdateAsync(Budget budget, CancellationToken ct = default);
    Task DeleteAsync(Budget budget, CancellationToken ct = default);
}
