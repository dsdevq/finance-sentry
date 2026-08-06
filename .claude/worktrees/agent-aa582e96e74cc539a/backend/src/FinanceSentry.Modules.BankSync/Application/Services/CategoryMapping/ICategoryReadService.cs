namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

using FinanceSentry.Modules.BankSync.Domain;

/// <summary>Read access to the canonical category reference list (for UI labels).</summary>
public interface ICategoryReadService
{
    /// <summary>Returns all categories ordered by <see cref="Category.SortOrder"/>.</summary>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}
