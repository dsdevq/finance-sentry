namespace FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Maps a lowercase substring of a transaction description to a canonical
/// <see cref="Category"/> key.
///
/// Some providers (notably TrueLayer for many EU banks) return no MCC and no category
/// name — the only signal is the free-text description ("Lidl Ireland Ltd", "amazon.ie").
/// This bridge table lets us recover a category from that text. It is the same pattern as
/// <see cref="MccCategory"/>: the taxonomy (Plaid PFC) is external; only this glue is ours,
/// and every row is editable at runtime so a manual correction wins over re-seeding.
/// </summary>
public class MerchantKeyword
{
    /// <summary>Surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Lowercase substring matched (case-insensitively) against the transaction description.
    /// Longer keywords are tried first so "uber eats" wins over "uber".
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>FK to <see cref="Category.Key"/>.</summary>
    public string CategoryKey { get; set; } = string.Empty;
}
