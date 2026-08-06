namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

public sealed class TrueLayerCategoryMapper
{
    private static readonly IReadOnlyDictionary<string, string> Lookup =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Food & drink
            ["Food & Dining"] = "FOOD_AND_DRINK",
            ["Food and Drink"] = "FOOD_AND_DRINK",
            ["Groceries"] = "FOOD_AND_DRINK",
            ["Restaurants"] = "FOOD_AND_DRINK",
            ["Catering"] = "FOOD_AND_DRINK",
            ["Coffee shops"] = "FOOD_AND_DRINK",
            ["Delivery"] = "FOOD_AND_DRINK",
            ["Fast Food"] = "FOOD_AND_DRINK",
            ["Bars"] = "FOOD_AND_DRINK",
            ["Food"] = "FOOD_AND_DRINK",
            // Shopping
            ["Shopping"] = "GENERAL_MERCHANDISE",
            ["General Merchandise"] = "GENERAL_MERCHANDISE",
            ["General"] = "GENERAL_MERCHANDISE",
            ["Clothing"] = "GENERAL_MERCHANDISE",
            ["Books"] = "GENERAL_MERCHANDISE",
            ["Books & Supplies"] = "GENERAL_MERCHANDISE",
            ["Electronics & Software"] = "GENERAL_MERCHANDISE",
            ["Hobbies"] = "GENERAL_MERCHANDISE",
            ["Sporting Goods"] = "GENERAL_MERCHANDISE",
            ["Personal Care"] = "GENERAL_MERCHANDISE",
            ["Laundry"] = "GENERAL_MERCHANDISE",
            ["Beauty"] = "GENERAL_MERCHANDISE",
            ["Spa & Massage"] = "GENERAL_MERCHANDISE",
            ["Pets"] = "GENERAL_MERCHANDISE",
            ["Sports"] = "MEDICAL",
            // Health
            ["Healthcare"] = "MEDICAL",
            ["Health & Fitness"] = "MEDICAL",
            ["Medical"] = "MEDICAL",
            ["Dentist"] = "MEDICAL",
            ["Doctor"] = "MEDICAL",
            ["Eye care"] = "MEDICAL",
            ["Pharmacy"] = "MEDICAL",
            ["Gym"] = "MEDICAL",
            // Utilities
            ["Bills & Utilities"] = "RENT_AND_UTILITIES",
            ["Utilities"] = "RENT_AND_UTILITIES",
            ["Television"] = "RENT_AND_UTILITIES",
            ["Home Phone"] = "RENT_AND_UTILITIES",
            ["Internet"] = "RENT_AND_UTILITIES",
            ["Mobile Phone"] = "RENT_AND_UTILITIES",
            ["Cable"] = "RENT_AND_UTILITIES",
            ["Telecom"] = "RENT_AND_UTILITIES",
            // Transport
            ["Auto & Transport"] = "TRANSPORTATION",
            ["Transport"] = "TRANSPORTATION",
            ["Transportation"] = "TRANSPORTATION",
            ["Auto Insurance"] = "TRANSPORTATION",
            ["Auto Payment"] = "TRANSPORTATION",
            ["Parking"] = "TRANSPORTATION",
            ["Public transport"] = "TRANSPORTATION",
            ["Service & Auto Parts"] = "TRANSPORTATION",
            ["Car Service"] = "TRANSPORTATION",
            ["Taxi"] = "TRANSPORTATION",
            ["Gas & Fuel"] = "TRANSPORTATION",
            // Travel
            ["Travel"] = "TRAVEL",
            ["Air Travel"] = "TRAVEL",
            ["Hotel"] = "TRAVEL",
            ["Rental Car & Taxi"] = "TRAVEL",
            ["Vacation"] = "TRAVEL",
            // Housing
            ["Home Improvement"] = "HOME_IMPROVEMENT",
            ["Home"] = "HOME_IMPROVEMENT",
            ["Home Services"] = "HOME_IMPROVEMENT",
            ["Rent"] = "HOME_IMPROVEMENT",
            ["Mortgage"] = "HOME_IMPROVEMENT",
            ["Secured loans"] = "HOME_IMPROVEMENT",
            ["Rent and Utilities"] = "HOME_IMPROVEMENT",
            ["Housing"] = "HOME_IMPROVEMENT",
            // Entertainment
            ["Entertainment"] = "ENTERTAINMENT",
            ["Arts"] = "ENTERTAINMENT",
            ["Music"] = "ENTERTAINMENT",
            ["Dating"] = "ENTERTAINMENT",
            ["Movies & DVDs"] = "ENTERTAINMENT",
            ["Social Club"] = "ENTERTAINMENT",
            ["Sport"] = "ENTERTAINMENT",
            ["Games"] = "ENTERTAINMENT",
            // Other / uncategorized
            ["Uncategorized"] = "UNCATEGORIZED",
            ["Check"] = "UNCATEGORIZED",
            ["Education"] = "UNCATEGORIZED",
            ["Tuition"] = "UNCATEGORIZED",
            ["Student Loan"] = "UNCATEGORIZED",
            ["Gifts & Donations"] = "UNCATEGORIZED",
            ["Gift"] = "UNCATEGORIZED",
            ["Charity"] = "UNCATEGORIZED",
            ["Investments"] = "UNCATEGORIZED",
            ["Equities"] = "UNCATEGORIZED",
            ["Bonds"] = "UNCATEGORIZED",
            ["Bank products"] = "UNCATEGORIZED",
            ["Retirement"] = "UNCATEGORIZED",
            ["Annuities"] = "UNCATEGORIZED",
            ["Real-estate"] = "UNCATEGORIZED",
            ["Fees & Charges"] = "UNCATEGORIZED",
            ["Service Fee"] = "UNCATEGORIZED",
            ["Late Fee"] = "UNCATEGORIZED",
            ["Finance Charge"] = "UNCATEGORIZED",
            ["ATM Fee"] = "UNCATEGORIZED",
            ["Bank Fee"] = "UNCATEGORIZED",
            ["Commissions"] = "UNCATEGORIZED",
            ["Business Services"] = "UNCATEGORIZED",
            ["Advertising"] = "UNCATEGORIZED",
            ["Financial Services"] = "UNCATEGORIZED",
            ["Office Supplies"] = "UNCATEGORIZED",
            ["Printing"] = "UNCATEGORIZED",
            ["Shipping"] = "UNCATEGORIZED",
            ["Legal"] = "UNCATEGORIZED",
            ["Personal Services"] = "UNCATEGORIZED",
            ["Advisory and Consulting"] = "UNCATEGORIZED",
            ["Lawyer"] = "UNCATEGORIZED",
            ["Repairs & Maintenance"] = "UNCATEGORIZED",
            ["Taxes"] = "UNCATEGORIZED",
            ["Pensions and Insurances"] = "UNCATEGORIZED",
            ["Pension payments"] = "UNCATEGORIZED",
            ["Life insurance"] = "UNCATEGORIZED",
            ["Buildings and contents insurance"] = "UNCATEGORIZED",
            ["Health insurance"] = "UNCATEGORIZED",
        };

    public string Map(IReadOnlyList<string>? classification)
    {
        if (classification is null || classification.Count == 0)
            return "UNCATEGORIZED";

        foreach (var raw in classification)
        {
            if (raw is null) continue;
            if (Lookup.TryGetValue(raw.Trim(), out var key))
                return key;
        }
        return "UNCATEGORIZED";
    }
}
