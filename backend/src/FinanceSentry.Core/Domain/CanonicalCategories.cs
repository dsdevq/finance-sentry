namespace FinanceSentry.Core.Domain;

/// <summary>One canonical taxonomy entry. Labels are display-only; keys are Plaid PFC primaries.</summary>
public record CategoryDefinition(string Key, string Label, int SortOrder);

/// <summary>
/// The canonical category taxonomy (Plaid PFC primaries + <see cref="CategoryKeys.Uncategorized"/>),
/// defined once here so BankSync (categorization + <c>categories</c> table seed) and Budgets
/// (limit validation + labels) never drift. Spend-relevant categories are ordered first.
/// </summary>
public static class CanonicalCategories
{
    public static readonly IReadOnlyList<CategoryDefinition> Definitions =
    [
        new(CategoryKeys.FoodAndDrink, "Food & Drink", 10),
        new(CategoryKeys.GeneralMerchandise, "Shopping", 20),
        new(CategoryKeys.Transportation, "Transport", 30),
        new(CategoryKeys.Travel, "Travel", 40),
        new(CategoryKeys.RentAndUtilities, "Bills & Utilities", 50),
        new(CategoryKeys.HomeImprovement, "Home", 60),
        new(CategoryKeys.Medical, "Health", 70),
        new(CategoryKeys.PersonalCare, "Personal Care", 80),
        new(CategoryKeys.Entertainment, "Entertainment", 90),
        new(CategoryKeys.GeneralServices, "Services", 100),
        new(CategoryKeys.GovernmentAndNonProfit, "Government & Non-Profit", 110),
        new(CategoryKeys.LoanPayments, "Loan Payments", 120),
        new(CategoryKeys.BankFees, "Bank Fees", 130),
        new(CategoryKeys.FamilySupport, "Family Support", 135),
        new(CategoryKeys.Income, "Income", 140),
        new(CategoryKeys.TransferIn, "Transfer In", 150),
        new(CategoryKeys.TransferOut, "Transfer Out", 160),
        new(CategoryKeys.Uncategorized, "Uncategorized", 999),
    ];
}
