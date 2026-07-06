namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

/// <summary>
/// Test double mirroring <c>CategoryResolver</c> semantics without a database:
/// Plaid primaries pass through (uppercased); MCCs classify via the range rules.
/// </summary>
internal sealed class StubCategoryResolver : ICategoryResolver
{
    public static readonly StubCategoryResolver Instance = new();

    public string ResolveMcc(int? mcc)
        => mcc is null ? CategoryKeys.Uncategorized : MccRangeClassifier.Classify(mcc.Value);

    public string ResolvePlaidPrimary(string? primary)
        => string.IsNullOrWhiteSpace(primary) ? CategoryKeys.Uncategorized : primary.Trim().ToUpperInvariant();

    public void Refresh()
    {
    }
}
