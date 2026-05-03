namespace FinanceSentry.Modules.Budgets.Application.Services;

using FinanceSentry.Modules.Budgets.Domain;

public class CategoryNormalizationService : ICategoryNormalizationService
{
    public string Normalize(string? rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
            return "other";

        var lower = rawCategory.ToLowerInvariant();
        if (lower == rawCategory && CategoryTaxonomy.ValidKeys.Contains(rawCategory))
            return rawCategory;

        foreach (var (key, rawValues) in CategoryTaxonomy.RawToKey)
        {
            foreach (var raw in rawValues)
            {
                if (raw.Equals(rawCategory, StringComparison.OrdinalIgnoreCase))
                    return key;
            }
        }

        return "other";
    }

    public string GetLabel(string categoryKey)
    {
        return CategoryTaxonomy.CategoryLabels.TryGetValue(categoryKey, out var label)
            ? label
            : "Other";
    }
}
