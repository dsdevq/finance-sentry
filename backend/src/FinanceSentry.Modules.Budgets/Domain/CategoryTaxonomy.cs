namespace FinanceSentry.Modules.Budgets.Domain;

public static class CategoryTaxonomy
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RawToKey =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["housing"] = ["Rent", "Mortgage & Rent", "Mortgage", "Home Improvement", "Home Services", "Real Estate"],
            ["food_and_drink"] = ["Food and Drink", "Restaurants", "Coffee Shop", "Groceries", "Fast Food", "Bakeries", "Food"],
            ["transport"] = ["Travel", "Gas Stations", "Taxi", "Public Transportation", "Parking", "Tolls", "Auto", "Automotive", "Car Service", "Ride Share"],
            ["shopping"] = ["Shops", "Clothing", "Electronics", "Sporting Goods", "Department Stores", "Online Marketplaces", "Shopping"],
            ["entertainment"] = ["Recreation", "Arts and Entertainment", "Games", "Movies and DVDs", "Music", "Nightlife", "Entertainment"],
            ["health"] = ["Healthcare", "Pharmacies", "Gyms and Fitness Centers", "Doctor", "Dentists", "Optometrists", "Health & Fitness"],
            ["utilities"] = ["Utilities", "Electric", "Gas", "Water", "Phone", "Internet", "Cable", "Telecom"],
            ["travel"] = ["Airlines and Aviation Services", "Hotels and Motels", "Car Rental", "Lodging", "Vacation Rentals"],
            ["other"] = [],
        };

    public static readonly IReadOnlySet<string> ValidKeys =
        new HashSet<string>(RawToKey.Keys, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<string, string> CategoryLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["housing"] = "Housing",
            ["food_and_drink"] = "Food & Drink",
            ["transport"] = "Transport",
            ["shopping"] = "Shopping",
            ["entertainment"] = "Entertainment",
            ["health"] = "Health & Fitness",
            ["utilities"] = "Utilities",
            ["travel"] = "Travel",
            ["other"] = "Other",
        };
}
