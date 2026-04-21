namespace FinanceSentry.Modules.BankSync.Application.Services;

public static class ProviderCategoryMapper
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plaid"] = "banking",
        ["monobank"] = "banking",
        ["binance"] = "crypto",
        ["ibkr"] = "brokerage",
    };

    public static string Map(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return "other";

        return _map.TryGetValue(provider, out var category) ? category : "other";
    }
}
