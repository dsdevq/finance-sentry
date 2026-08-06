namespace FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Seed rows for the <c>categories</c> table. Keys are Plaid PFC primaries (the external
/// taxonomy we adopt) plus <see cref="CategoryKeys.Uncategorized"/>. Labels are display-only.
/// Spend-relevant categories are ordered first.
/// </summary>
public static class CategorySeedData
{
    public static readonly IReadOnlyList<Category> Categories =
    [
        new() { Key = CategoryKeys.FoodAndDrink, Label = "Food & Drink", SortOrder = 10 },
        new() { Key = CategoryKeys.GeneralMerchandise, Label = "Shopping", SortOrder = 20 },
        new() { Key = CategoryKeys.Transportation, Label = "Transport", SortOrder = 30 },
        new() { Key = CategoryKeys.Travel, Label = "Travel", SortOrder = 40 },
        new() { Key = CategoryKeys.RentAndUtilities, Label = "Bills & Utilities", SortOrder = 50 },
        new() { Key = CategoryKeys.HomeImprovement, Label = "Home", SortOrder = 60 },
        new() { Key = CategoryKeys.Medical, Label = "Health", SortOrder = 70 },
        new() { Key = CategoryKeys.PersonalCare, Label = "Personal Care", SortOrder = 80 },
        new() { Key = CategoryKeys.Entertainment, Label = "Entertainment", SortOrder = 90 },
        new() { Key = CategoryKeys.GeneralServices, Label = "Services", SortOrder = 100 },
        new() { Key = CategoryKeys.GovernmentAndNonProfit, Label = "Government & Non-Profit", SortOrder = 110 },
        new() { Key = CategoryKeys.LoanPayments, Label = "Loan Payments", SortOrder = 120 },
        new() { Key = CategoryKeys.BankFees, Label = "Bank Fees", SortOrder = 130 },
        new() { Key = CategoryKeys.Income, Label = "Income", SortOrder = 140 },
        new() { Key = CategoryKeys.TransferIn, Label = "Transfer In", SortOrder = 150 },
        new() { Key = CategoryKeys.TransferOut, Label = "Transfer Out", SortOrder = 160 },
        new() { Key = CategoryKeys.Uncategorized, Label = "Uncategorized", SortOrder = 999 },
    ];
}
