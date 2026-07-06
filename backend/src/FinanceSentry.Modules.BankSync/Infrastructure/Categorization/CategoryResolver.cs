namespace FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Singleton resolver backed by an in-memory snapshot of the reference tables.
/// The snapshot loads lazily on first use (thread-safe) and can be dropped via
/// <see cref="Refresh"/> after the tables are edited.
/// </summary>
public sealed class CategoryResolver(IServiceScopeFactory scopeFactory) : ICategoryResolver
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly object _gate = new();

    private IReadOnlyDictionary<int, string>? _mccToKey;
    private IReadOnlySet<string>? _validKeys;

    public string ResolveMcc(int? mcc)
    {
        EnsureLoaded();
        if (mcc is null)
            return CategoryKeys.Uncategorized;

        return _mccToKey!.TryGetValue(mcc.Value, out var key) ? key : CategoryKeys.Uncategorized;
    }

    public string ResolvePlaidPrimary(string? primary)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(primary))
            return CategoryKeys.Uncategorized;

        var normalized = primary.Trim().ToUpperInvariant();
        return _validKeys!.Contains(normalized) ? normalized : CategoryKeys.Uncategorized;
    }

    public void Refresh()
    {
        lock (_gate)
        {
            _mccToKey = null;
            _validKeys = null;
        }
    }

    private void EnsureLoaded()
    {
        if (_mccToKey is not null && _validKeys is not null)
            return;

        lock (_gate)
        {
            if (_mccToKey is not null && _validKeys is not null)
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankSyncDbContext>();

            _validKeys = db.Categories.AsNoTracking()
                .Select(c => c.Key)
                .ToHashSet(StringComparer.Ordinal);

            _mccToKey = db.MccCategories.AsNoTracking()
                .ToDictionary(m => m.Mcc, m => m.CategoryKey);
        }
    }
}
