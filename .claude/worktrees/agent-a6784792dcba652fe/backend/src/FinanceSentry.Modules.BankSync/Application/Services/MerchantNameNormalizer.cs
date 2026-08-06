namespace FinanceSentry.Modules.BankSync.Application.Services;

using System.Text.RegularExpressions;

public static class MerchantNameNormalizer
{
    private static readonly string[] DomainSuffixes = [".com", ".net", ".io", ".co", ".org"];
    private static readonly Regex TrailingNumericPattern = new(@"[\s\-_*#]+\d[\d\s\-_]*$", RegexOptions.Compiled);
    private static readonly Regex CollapseSpacesPattern = new(@"\s+", RegexOptions.Compiled);

    // Bank statements spell the same recurring merchant differently every month
    // (e.g. "Anthropic* Claude Sub", "Claude.ai Subscription", "Anthropic Ireland"),
    // which fragments them below the recurrence threshold. Collapse known brands to
    // one canonical key so their charges group together. Keep this list narrow —
    // only brands whose descriptions actually vary — to avoid over-merging.
    private static readonly (string Keyword, string Canonical)[] BrandAliases =
    [
        ("anthropic", "claude"),
        ("claude", "claude"),
        ("openai", "openai"),
        ("chatgpt", "openai"),
    ];

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unknown";

        var result = input.Trim().ToLowerInvariant();

        if (result.StartsWith("paypal*", StringComparison.Ordinal))
            result = result["paypal*".Length..];

        foreach (var suffix in DomainSuffixes)
        {
            if (result.EndsWith(suffix, StringComparison.Ordinal))
            {
                result = result[..^suffix.Length];
                break;
            }
        }

        result = result.TrimStart('*', '#', ' ');

        result = TrailingNumericPattern.Replace(result, string.Empty);

        result = CollapseSpacesPattern.Replace(result, " ").Trim();

        if (string.IsNullOrWhiteSpace(result))
            return "unknown";

        foreach (var (keyword, canonical) in BrandAliases)
        {
            if (result.Contains(keyword, StringComparison.Ordinal))
                return canonical;
        }

        return result;
    }

    public static string GetDisplayName(IEnumerable<string?> rawNames)
    {
        var grouped = rawNames
            .Where(n => n is not null)
            .GroupBy(n => n)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return grouped?.Key ?? "unknown";
    }
}
