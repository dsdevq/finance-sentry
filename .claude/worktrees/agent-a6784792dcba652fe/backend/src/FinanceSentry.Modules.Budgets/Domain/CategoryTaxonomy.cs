namespace FinanceSentry.Modules.Budgets.Domain;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

/// <summary>
/// Budgets adopt the same canonical taxonomy as transaction categorization (Plaid PFC
/// primaries, defined once in <see cref="CategorySeedData"/>). This type only adapts that
/// single source of truth for the Budgets module, plus a legacy-key remap for data written
/// before the M005 taxonomy migration.
/// </summary>
public static class CategoryTaxonomy
{
    public static readonly IReadOnlyDictionary<string, string> CategoryLabels =
        CategorySeedData.Categories.ToDictionary(c => c.Key, c => c.Label, StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> ValidKeys =
        CategorySeedData.Categories.Select(c => c.Key).ToHashSet(StringComparer.Ordinal);

    /// <summary>Legacy lowercase keys → adopted keys (for budgets created before M005).</summary>
    public static readonly IReadOnlyDictionary<string, string> LegacyKeyMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["food_and_drink"] = CategoryKeys.FoodAndDrink,
            ["transport"] = CategoryKeys.Transportation,
            ["shopping"] = CategoryKeys.GeneralMerchandise,
            ["entertainment"] = CategoryKeys.Entertainment,
            ["health"] = CategoryKeys.Medical,
            ["utilities"] = CategoryKeys.RentAndUtilities,
            ["travel"] = CategoryKeys.Travel,
            ["housing"] = CategoryKeys.HomeImprovement,
            ["other"] = CategoryKeys.Uncategorized,
        };
}
