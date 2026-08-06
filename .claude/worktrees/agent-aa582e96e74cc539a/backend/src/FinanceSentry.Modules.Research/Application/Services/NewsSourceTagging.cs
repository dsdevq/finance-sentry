namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// Pure tagging rules for registered news sources (feature 030, FR-008). An article is tagged with a
/// source's thesis when the source is registered to one AND (the source has no keyword filter OR the
/// article title/summary matches a keyword). Keywords gate thesis tagging only — they never drop an
/// article, so market-wide breadth is preserved.
/// </summary>
public static class NewsSourceTagging
{
    public static bool MatchesKeywords(NewsSource source, string title, string? summary)
    {
        if (source.Keywords.Count == 0)
        {
            return true;
        }

        var haystack = $"{title}\n{summary}";
        return source.Keywords.Any(k =>
            !string.IsNullOrWhiteSpace(k) &&
            haystack.Contains(k.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<Guid> ResolveThesisIds(NewsSource source, string title, string? summary)
    {
        if (source.ThesisId is { } thesisId && MatchesKeywords(source, title, summary))
        {
            return [thesisId];
        }

        return [];
    }
}
