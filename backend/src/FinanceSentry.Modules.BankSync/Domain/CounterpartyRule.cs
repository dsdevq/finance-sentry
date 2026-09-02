namespace FinanceSentry.Modules.BankSync.Domain;

using FinanceSentry.Core.Domain;

/// <summary>
/// A single matching rule for a <see cref="Counterparty"/>.
/// A transaction matches if the specified field contains the pattern (case-insensitive).
/// </summary>
public class CounterpartyRule : Entity
{
    /// <summary>FK to the owning counterparty.</summary>
    public Guid CounterpartyId { get; set; }

    /// <summary>
    /// Which transaction field to test.
    /// "description_contains" — checks <c>Transaction.Description</c>.
    /// "merchant_name_contains" — checks <c>Transaction.MerchantName</c>.
    /// </summary>
    public string MatchType { get; set; } = string.Empty;

    /// <summary>Substring pattern (case-insensitive).</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>Navigation back to owning counterparty.</summary>
    public Counterparty? Counterparty { get; set; }
}
