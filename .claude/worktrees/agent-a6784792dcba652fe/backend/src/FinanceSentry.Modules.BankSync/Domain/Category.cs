namespace FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Canonical spending category. Keys mirror Plaid Personal Finance Category (PFC)
/// primaries verbatim (the external source of truth) plus one synthetic
/// <c>UNCATEGORIZED</c> fallback for transactions we cannot classify.
///
/// Reference data: seeded from <see cref="Category"/> constants at startup and
/// editable at runtime. We do NOT author the taxonomy — the key set is Plaid's;
/// only the human-facing <see cref="Label"/> is display sugar.
/// </summary>
public class Category
{
    /// <summary>
    /// Primary key. Plaid PFC primary value (e.g. "FOOD_AND_DRINK") or "UNCATEGORIZED".
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display label (e.g. "Food &amp; Drink"). Display-only.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Ordering hint for UI (spend-relevant categories first).
    /// </summary>
    public int SortOrder { get; set; }
}
