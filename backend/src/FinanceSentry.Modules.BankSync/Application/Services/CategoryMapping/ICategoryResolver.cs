namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

/// <summary>
/// Resolves a raw provider category signal into a canonical category key, using the
/// runtime-editable <c>categories</c> / <c>mcc_categories</c> reference tables.
/// </summary>
public interface ICategoryResolver
{
    /// <summary>
    /// Resolves a Monobank/card MCC via the <c>mcc_categories</c> table.
    /// Returns UNCATEGORIZED when the MCC is null or unmapped.
    /// </summary>
    string ResolveMcc(int? mcc);

    /// <summary>
    /// Resolves a Plaid PFC primary (which is itself a canonical key). Returns the
    /// canonical key when known, otherwise UNCATEGORIZED.
    /// </summary>
    string ResolvePlaidPrimary(string? primary);

    /// <summary>Drops the in-memory cache so the next resolve reloads from the database.</summary>
    void Refresh();
}
