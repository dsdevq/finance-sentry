namespace FinanceSentry.Modules.Analytics.Tests;

using FinanceSentry.Modules.Analytics.Application.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// The schema card must expose exactly the curated views and nothing else (feature 033, US2, SC-005).
/// </summary>
public sealed class CuratedSchemaTests
{
    private readonly CuratedSchema _schema = new();

    [Fact]
    public void Get_ReturnsExactlyTheFiveCuratedViews()
    {
        var names = _schema.Get().Views.Select(v => v.Name).ToList();

        names.Should().BeEquivalentTo(
        [
            "analytics.v_transactions",
            "analytics.v_holdings",
            "analytics.v_analyst_actions",
            "analytics.v_net_worth_daily",
            "analytics.v_budgets",
        ]);
    }

    [Fact]
    public void Get_ExposesOnlyCuratedViews_NeverRawTables()
    {
        _schema.Get().Views.Should().OnlyContain(v => v.Name.StartsWith("analytics.v_"));
    }

    [Fact]
    public void Get_TransactionsView_HasDocumentedColumns()
    {
        var view = _schema.Get().Views.Single(v => v.Name == "analytics.v_transactions");

        view.Columns.Select(c => c.Name).Should().BeEquivalentTo(
            ["date", "amount", "currency", "merchant", "category", "account_name", "direction"]);
    }

    [Fact]
    public void Get_EveryViewHasAtLeastOneColumnAndAPurpose()
    {
        foreach (var view in _schema.Get().Views)
        {
            view.Columns.Should().NotBeEmpty();
            view.Purpose.Should().NotBeNullOrWhiteSpace();
        }
    }
}
