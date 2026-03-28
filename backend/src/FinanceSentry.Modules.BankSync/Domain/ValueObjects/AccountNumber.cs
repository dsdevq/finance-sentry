namespace FinanceSentry.Modules.BankSync.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the last 4 digits of a bank account number.
/// Stores only last 4 digits for PCI compliance — never the full account number.
/// Equality is by value (the 4-digit string).
/// </summary>
public sealed record AccountNumber
{
    public string Last4 { get; }

    public AccountNumber(string last4)
    {
        if (string.IsNullOrWhiteSpace(last4))
            throw new ArgumentException("Account number last 4 digits cannot be empty.", nameof(last4));

        if (last4.Length != 4)
            throw new ArgumentException(
                $"AccountNumber must be exactly 4 digits (PCI compliance). Got {last4.Length}.", nameof(last4));

        if (!last4.All(char.IsDigit))
            throw new ArgumentException("AccountNumber last 4 must contain only digits.", nameof(last4));

        Last4 = last4;
    }

    /// <summary>Returns the masked display form: ••••1234</summary>
    public string Masked => $"••••{Last4}";

    public override string ToString() => Masked;
}
