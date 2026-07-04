namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

public sealed class PlaidCategoryMapper : IProviderCategoryMapper
{
    private static readonly IReadOnlyDictionary<string, string> Lookup =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FOOD_AND_DRINK"] = "food_and_drink",
            ["GENERAL_MERCHANDISE"] = "shopping",
            ["PERSONAL_CARE"] = "shopping",
            ["ENTERTAINMENT"] = "entertainment",
            ["TRAVEL"] = "travel",
            ["TRANSPORTATION"] = "transport",
            ["RENT_AND_UTILITIES"] = "utilities",
            ["HOME_IMPROVEMENT"] = "housing",
            ["MEDICAL"] = "health",
            ["INCOME"] = "other",
            ["TRANSFER_IN"] = "other",
            ["TRANSFER_OUT"] = "other",
            ["LOAN_PAYMENTS"] = "other",
            ["GENERAL_SERVICES"] = "other",
            ["GOVERNMENT_AND_NON_PROFIT"] = "other",
            ["BANK_FEES"] = "other",
            ["CASH_ADVANCES_AND_MISC"] = "other",
            ["INVESTMENT"] = "other",
        };

    public string Map(string? rawCategory)
    {
        if (rawCategory is null) return "other";
        return Lookup.TryGetValue(rawCategory, out var key) ? key : "other";
    }
}
