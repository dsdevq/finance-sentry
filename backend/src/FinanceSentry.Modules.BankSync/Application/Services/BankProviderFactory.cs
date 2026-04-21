namespace FinanceSentry.Modules.BankSync.Application.Services;

using FinanceSentry.Modules.BankSync.Domain.Interfaces;

public class BankProviderFactory(IEnumerable<IBankProvider> providers) : IBankProviderFactory
{
    private readonly Dictionary<string, IBankProvider> _map = providers.ToDictionary(p => p.ProviderName);

    public IBankProvider Resolve(string provider)
    {
        if (_map.TryGetValue(provider, out var p))
            return p;
        throw new InvalidOperationException($"No IBankProvider registered for provider '{provider}'.");
    }
}
