namespace FinanceSentry.Modules.BankSync.Application.Services;

public static class CurrencyConverter
{
    private static readonly Dictionary<string, decimal> _rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1.00m,
        ["EUR"] = 1.08m,
        ["GBP"] = 1.27m,
        ["UAH"] = 0.024m,
    };

    public static decimal ToUsd(decimal amount, string currency)
    {
        if (_rates.TryGetValue(currency, out var rate))
            return amount * rate;

        return amount;
    }
}
