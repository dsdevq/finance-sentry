namespace FinanceSentry.Core.Domain;

/// <summary>
/// Canonical category keys. The 16 values below are Plaid Personal Finance Category
/// primaries (verbatim), so Plaid transactions map 1:1 with zero glue. <see cref="Uncategorized"/>
/// is the one synthetic key we add for transactions that carry no usable signal.
/// Shared-kernel vocabulary: BankSync categorizes into these keys, Budgets budgets against them.
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

    /// <summary>
    /// Gross outbound payments to family counterparties (per direction — support sent is
    /// never netted against rent received).
    /// Produced by <c>CounterpartyClassificationService</c>; never stored on a
    /// transaction row — it is a computed bucket that appears in top-categories output.
    /// </summary>
    public const string FamilySupport = "FAMILY_SUPPORT";

    /// <summary>
    /// True for the two directional-transfer keys. Transfers move money between a user's own
    /// accounts (incl. single-sided ones like savings-jar top-ups whose counterpart account is
    /// not synced), so they must never be counted as spend or cash-flow.
    /// </summary>
    public static bool IsTransfer(string? category) =>
        category == TransferIn || category == TransferOut;
}
