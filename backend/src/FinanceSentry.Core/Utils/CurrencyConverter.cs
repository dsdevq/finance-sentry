namespace FinanceSentry.Core.Utils;

public static class CurrencyConverter
{
    private static readonly Dictionary<string, decimal> Rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1.00m,
        ["EUR"] = 1.08m,
        ["GBP"] = 1.27m,
        ["UAH"] = 0.024m,
    };

    public static decimal ToUsd(decimal amount, string currency)
    {
        if (Rates.TryGetValue(currency, out var rate))
            return amount * rate;

        return amount;
    }
}
