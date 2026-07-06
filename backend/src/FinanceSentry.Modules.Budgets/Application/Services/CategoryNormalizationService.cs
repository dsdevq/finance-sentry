namespace FinanceSentry.Modules.Budgets.Application.Services;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.Budgets.Domain;

public class CategoryNormalizationService : ICategoryNormalizationService
{
    public string Normalize(string? rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
            return CategoryKeys.Uncategorized;

        var upper = rawCategory.Trim().ToUpperInvariant();
        if (CategoryTaxonomy.ValidKeys.Contains(upper))
            return upper;

        if (CategoryTaxonomy.LegacyKeyMap.TryGetValue(rawCategory.Trim(), out var mapped))
            return mapped;

        return CategoryKeys.Uncategorized;
    }

    public string GetLabel(string categoryKey)
    {
        var normalized = Normalize(categoryKey);
        return CategoryTaxonomy.CategoryLabels.TryGetValue(normalized, out var label)
            ? label
            : "Uncategorized";
    }
}
