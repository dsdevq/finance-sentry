namespace FinanceSentry.Modules.BankSync.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing an amount in a specific currency.
/// Equality is by value (amount + currency), not reference.
/// Currency codes must be valid ISO 4217 (3 uppercase letters).
/// Money values in different currencies cannot be compared or added.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private static readonly HashSet<string> KnownCurrencies =
        ["EUR", "USD", "GBP", "UAH", "CHF", "PLN", "CZK", "HUF", "SEK", "NOK", "DKK"];

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative. Use TransactionType to indicate direction.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-character ISO 4217 code.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>Adds two Money values. Both must have the same currency.</summary>
    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    /// <summary>Subtracts two Money values. Both must have the same currency.</summary>
    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money Zero(string currency) => new(0m, currency);

    public override string ToString() => $"{Amount:F2} {Currency}";

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on Money values with different currencies: {a.Currency} vs {b.Currency}");
    }
}
