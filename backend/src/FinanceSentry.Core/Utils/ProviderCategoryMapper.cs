namespace FinanceSentry.Core.Utils;

public static class ProviderCategoryMapper
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plaid"] = "banking",
        ["monobank"] = "banking",
        ["binance"] = "crypto",
        ["ibkr"] = "brokerage",
    };

    public static string GetCategory(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return "other";

        return Map.TryGetValue(provider, out var category) ? category : "other";
    }
}
