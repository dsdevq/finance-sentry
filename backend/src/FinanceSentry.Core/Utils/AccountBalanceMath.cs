namespace FinanceSentry.Core.Utils;

/// <summary>
/// Sign convention for balance aggregation. A credit account's provider balance is the
/// amount owed (TrueLayer CREDIT_CARD semantics), so it is a liability: it enters any
/// net total negated, while per-account display keeps the raw provider value. Apply at
/// every aggregation boundary, the same way <see cref="CurrencyConverter.ToUsd"/> is
/// the single currency-conversion point.
/// </summary>
public static class AccountBalanceMath
{
    public const string CreditAccountType = "credit";

    public static bool IsLiability(string? accountType)
        => string.Equals(accountType, CreditAccountType, StringComparison.OrdinalIgnoreCase);

    public static decimal SignedForNetTotal(string? accountType, decimal amount)
        => IsLiability(accountType) ? -amount : amount;
}
