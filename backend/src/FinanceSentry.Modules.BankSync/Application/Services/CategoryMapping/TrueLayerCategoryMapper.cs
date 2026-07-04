namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

public sealed class TrueLayerCategoryMapper
{
    private static readonly IReadOnlyDictionary<string, string> Lookup =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Food & drink
            ["Food & Dining"] = "food_and_drink",
            ["Food and Drink"] = "food_and_drink",
            ["Groceries"] = "food_and_drink",
            ["Restaurants"] = "food_and_drink",
            ["Catering"] = "food_and_drink",
            ["Coffee shops"] = "food_and_drink",
            ["Delivery"] = "food_and_drink",
            ["Fast Food"] = "food_and_drink",
            ["Bars"] = "food_and_drink",
            ["Food"] = "food_and_drink",
            // Shopping
            ["Shopping"] = "shopping",
            ["General Merchandise"] = "shopping",
            ["General"] = "shopping",
            ["Clothing"] = "shopping",
            ["Books"] = "shopping",
            ["Books & Supplies"] = "shopping",
            ["Electronics & Software"] = "shopping",
            ["Hobbies"] = "shopping",
            ["Sporting Goods"] = "shopping",
            ["Personal Care"] = "shopping",
            ["Laundry"] = "shopping",
            ["Beauty"] = "shopping",
            ["Spa & Massage"] = "shopping",
            ["Pets"] = "shopping",
            ["Sports"] = "health",
            // Health
            ["Healthcare"] = "health",
            ["Health & Fitness"] = "health",
            ["Medical"] = "health",
            ["Dentist"] = "health",
            ["Doctor"] = "health",
            ["Eye care"] = "health",
            ["Pharmacy"] = "health",
            ["Gym"] = "health",
            // Utilities
            ["Bills & Utilities"] = "utilities",
            ["Utilities"] = "utilities",
            ["Television"] = "utilities",
            ["Home Phone"] = "utilities",
            ["Internet"] = "utilities",
            ["Mobile Phone"] = "utilities",
            ["Cable"] = "utilities",
            ["Telecom"] = "utilities",
            // Transport
            ["Auto & Transport"] = "transport",
            ["Transport"] = "transport",
            ["Transportation"] = "transport",
            ["Auto Insurance"] = "transport",
            ["Auto Payment"] = "transport",
            ["Parking"] = "transport",
            ["Public transport"] = "transport",
            ["Service & Auto Parts"] = "transport",
            ["Car Service"] = "transport",
            ["Taxi"] = "transport",
            ["Gas & Fuel"] = "transport",
            // Travel
            ["Travel"] = "travel",
            ["Air Travel"] = "travel",
            ["Hotel"] = "travel",
            ["Rental Car & Taxi"] = "travel",
            ["Vacation"] = "travel",
            // Housing
            ["Home Improvement"] = "housing",
            ["Home"] = "housing",
            ["Home Services"] = "housing",
            ["Rent"] = "housing",
            ["Mortgage"] = "housing",
            ["Secured loans"] = "housing",
            ["Rent and Utilities"] = "housing",
            ["Housing"] = "housing",
            // Entertainment
            ["Entertainment"] = "entertainment",
            ["Arts"] = "entertainment",
            ["Music"] = "entertainment",
            ["Dating"] = "entertainment",
            ["Movies & DVDs"] = "entertainment",
            ["Social Club"] = "entertainment",
            ["Sport"] = "entertainment",
            ["Games"] = "entertainment",
            // Other / uncategorized
            ["Uncategorized"] = "other",
            ["Check"] = "other",
            ["Education"] = "other",
            ["Tuition"] = "other",
            ["Student Loan"] = "other",
            ["Gifts & Donations"] = "other",
            ["Gift"] = "other",
            ["Charity"] = "other",
            ["Investments"] = "other",
            ["Equities"] = "other",
            ["Bonds"] = "other",
            ["Bank products"] = "other",
            ["Retirement"] = "other",
            ["Annuities"] = "other",
            ["Real-estate"] = "other",
            ["Fees & Charges"] = "other",
            ["Service Fee"] = "other",
            ["Late Fee"] = "other",
            ["Finance Charge"] = "other",
            ["ATM Fee"] = "other",
            ["Bank Fee"] = "other",
            ["Commissions"] = "other",
            ["Business Services"] = "other",
            ["Advertising"] = "other",
            ["Financial Services"] = "other",
            ["Office Supplies"] = "other",
            ["Printing"] = "other",
            ["Shipping"] = "other",
            ["Legal"] = "other",
            ["Personal Services"] = "other",
            ["Advisory and Consulting"] = "other",
            ["Lawyer"] = "other",
            ["Repairs & Maintenance"] = "other",
            ["Taxes"] = "other",
            ["Pensions and Insurances"] = "other",
            ["Pension payments"] = "other",
            ["Life insurance"] = "other",
            ["Buildings and contents insurance"] = "other",
            ["Health insurance"] = "other",
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
