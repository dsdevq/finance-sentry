namespace FinanceSentry.Modules.BankSync.Application.Services;

using System.Text.RegularExpressions;

public static class MerchantNameNormalizer
{
    private static readonly string[] DomainSuffixes = [".com", ".net", ".io", ".co", ".org"];
    private static readonly Regex TrailingNumericPattern = new(@"[\s\-_*#]+\d[\d\s\-_]*$", RegexOptions.Compiled);
    private static readonly Regex CollapseSpacesPattern = new(@"\s+", RegexOptions.Compiled);

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

        return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
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
