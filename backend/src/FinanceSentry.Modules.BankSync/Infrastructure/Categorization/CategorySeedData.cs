namespace FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

using FinanceSentry.Core.Domain;
using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Seed rows for the <c>categories</c> table, projected from the shared-kernel
/// <see cref="CanonicalCategories"/> taxonomy (Plaid PFC primaries plus
/// <see cref="CategoryKeys.Uncategorized"/>).
/// </summary>
public static class CategorySeedData
{
    public static readonly IReadOnlyList<Category> Categories =
        CanonicalCategories.Definitions
            .Select(d => new Category { Key = d.Key, Label = d.Label, SortOrder = d.SortOrder })
            .ToList();
}
