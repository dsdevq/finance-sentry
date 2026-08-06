namespace FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Canonical category keys. The 16 values below are Plaid Personal Finance Category
/// primaries (verbatim), so Plaid transactions map 1:1 with zero glue. <see cref="Uncategorized"/>
/// is the one synthetic key we add for transactions that carry no usable signal.
/// </summary>
public static class CategoryKeys
{
    public const string Income = "INCOME";
    public const string TransferIn = "TRANSFER_IN";
    public const string TransferOut = "TRANSFER_OUT";
    public const string LoanPayments = "LOAN_PAYMENTS";
    public const string BankFees = "BANK_FEES";
    public const string Entertainment = "ENTERTAINMENT";
    public const string FoodAndDrink = "FOOD_AND_DRINK";
    public const string GeneralMerchandise = "GENERAL_MERCHANDISE";
    public const string HomeImprovement = "HOME_IMPROVEMENT";
    public const string Medical = "MEDICAL";
    public const string PersonalCare = "PERSONAL_CARE";
    public const string GeneralServices = "GENERAL_SERVICES";
    public const string GovernmentAndNonProfit = "GOVERNMENT_AND_NON_PROFIT";
    public const string Transportation = "TRANSPORTATION";
    public const string Travel = "TRAVEL";
    public const string RentAndUtilities = "RENT_AND_UTILITIES";

    /// <summary>Synthetic fallback for transactions with no resolvable category.</summary>
    public const string Uncategorized = "UNCATEGORIZED";
}
