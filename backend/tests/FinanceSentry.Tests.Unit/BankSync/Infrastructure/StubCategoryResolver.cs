namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Core.Domain;
using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

/// <summary>
/// Test double mirroring <c>CategoryResolver</c> semantics without a database:
/// Canonical keys pass through (uppercased); MCCs classify via the range rules.
/// </summary>
internal sealed class StubCategoryResolver : ICategoryResolver
{
    public static readonly StubCategoryResolver Instance = new();

    public string ResolveMcc(int? mcc)
        => mcc is null ? CategoryKeys.Uncategorized : MccRangeClassifier.Classify(mcc.Value);

    public string ResolveCanonicalKey(string? primary)
        => string.IsNullOrWhiteSpace(primary) ? CategoryKeys.Uncategorized : primary.Trim().ToUpperInvariant();

    // Minimal keyword set + transfer prefix so adapter/service tests can exercise the fallback.
    public string ResolveDescription(string? description)
    {
        return TryResolveKeyword(description)
            ?? TransferDescriptionClassifier.Resolve(description)
            ?? CategoryKeys.Uncategorized;
    }

    public string? TryResolveKeyword(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;
        var h = description.ToLowerInvariant();
        if (h.Contains("lidl") || h.Contains("tesco"))
            return CategoryKeys.FoodAndDrink;
        if (h.Contains("amazon"))
            return CategoryKeys.GeneralMerchandise;
        if (h.Contains("погашення"))
            return CategoryKeys.LoanPayments;
        return null;
    }

    public void Refresh()
    {
    }
}
