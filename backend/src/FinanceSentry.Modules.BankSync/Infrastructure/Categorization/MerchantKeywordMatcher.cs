namespace FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Pure matching logic shared by <see cref="CategoryResolver"/> and its tests: matches a
/// free-text description (case-insensitively) against a keyword list, longest keyword first
/// so a more specific match ("uber eats") wins over a shorter substring ("uber").
/// </summary>
public static class MerchantKeywordMatcher
{
    /// <summary>
    /// Orders keywords longest-first and lowercases them for matching. Call once at load time.
    /// </summary>
    public static IReadOnlyList<(string Keyword, string CategoryKey)> Prepare(
        IEnumerable<(string Keyword, string CategoryKey)> keywords)
        => keywords
            .Select(k => (Keyword: k.Keyword.ToLowerInvariant(), k.CategoryKey))
            .OrderByDescending(k => k.Keyword.Length)
            .ToList();

    /// <summary>
    /// Returns the category of the first (longest) keyword contained in the description,
    /// or UNCATEGORIZED when none match. <paramref name="prepared"/> must come from
    /// <see cref="Prepare"/> (lowercased, longest-first).
    /// </summary>
    public static string Resolve(
        string? description, IReadOnlyList<(string Keyword, string CategoryKey)> prepared)
    {
        if (string.IsNullOrWhiteSpace(description))
            return CategoryKeys.Uncategorized;

        var haystack = description.ToLowerInvariant();
        foreach (var (keyword, categoryKey) in prepared)
        {
            if (haystack.Contains(keyword, StringComparison.Ordinal))
                return categoryKey;
        }
        return CategoryKeys.Uncategorized;
    }
}
