namespace FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Maps an ISO 18245 Merchant Category Code (MCC) to a canonical <see cref="Category"/> key.
///
/// Monobank (and card networks generally) only return a raw MCC integer — no category
/// name — so this bridge table is the one piece of mapping we necessarily own. It is
/// seeded from the public greggles/mcc-codes dataset (the <see cref="Description"/>) with
/// the category assigned by ISO numeric-range rules (see <c>MccRangeClassifier</c>).
/// Rows are editable at runtime; a manual edit wins over re-seeding.
/// </summary>
public class MccCategory
{
    /// <summary>
    /// Primary key. The ISO 18245 merchant category code (e.g. 5812).
    /// </summary>
    public int Mcc { get; set; }

    /// <summary>
    /// FK to <see cref="Category.Key"/>.
    /// </summary>
    public string CategoryKey { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable MCC description from the source dataset (traceability).
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
