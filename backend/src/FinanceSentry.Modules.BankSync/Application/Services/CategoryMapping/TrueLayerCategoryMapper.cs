namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

public class TrueLayerCategoryMapper
{
    private static readonly IReadOnlyDictionary<string, string> Lookup =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bills and Utilities"] = "utilities",
            ["Utilities"] = "utilities",
            ["Entertainment"] = "entertainment",
            ["Food & Dining"] = "food_and_drink",
            ["Food and Drink"] = "food_and_drink",
            ["Restaurants"] = "food_and_drink",
            ["Groceries"] = "food_and_drink",
            ["General Merchandise"] = "shopping",
            ["Shopping"] = "shopping",
            ["Personal Care"] = "shopping",
            ["Healthcare"] = "health",
            ["Medical"] = "health",
            ["Transport"] = "transport",
            ["Transportation"] = "transport",
            ["Travel"] = "travel",
            ["Home Improvement"] = "housing",
            ["Rent and Utilities"] = "housing",
            ["Housing"] = "housing",
        };

    public string Map(IReadOnlyList<string>? classification)
    {
        if (classification is null || classification.Count == 0)
            return "other";

        foreach (var raw in classification)
        {
            if (raw is null) continue;
            if (Lookup.TryGetValue(raw.Trim(), out var key))
                return key;
        }
        return "other";
    }
}
