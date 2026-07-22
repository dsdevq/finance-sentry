namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Infrastructure.Sources;
using FluentAssertions;
using Xunit;

/// <summary>
/// External-contract test (constitution-mandated) for Yahoo's
/// <c>quoteSummary?modules=upgradeDowngradeHistory</c> JSON shape (feature 030, T016). Asserts the
/// parser reads firm / fromGrade / toGrade / action / epochGradeDate from the documented path.
/// </summary>
public sealed class YahooAnalystActionsContractTests
{
    private const string SampleJson = """
    {
      "quoteSummary": {
        "result": [
          {
            "upgradeDowngradeHistory": {
              "history": [
                { "epochGradeDate": 1721433600, "firm": "Morgan Stanley", "toGrade": "Overweight", "fromGrade": "Equal-Weight", "action": "up" },
                { "epochGradeDate": 1721347200, "firm": "Goldman Sachs", "toGrade": "Neutral", "fromGrade": "Buy", "action": "down" },
                { "epochGradeDate": 1721260800, "firm": "Wedbush", "toGrade": "Outperform", "fromGrade": "", "action": "init" }
              ]
            }
          }
        ],
        "error": null
      }
    }
    """;

    [Fact]
    public void Parse_reads_history_rows_with_firm_grades_and_action()
    {
        var actions = YahooAnalystActionsSource.Parse(SampleJson, "mu");

        actions.Should().HaveCount(3);

        var upgrade = actions[0];
        upgrade.Ticker.Should().Be("MU");
        upgrade.Firm.Should().Be("Morgan Stanley");
        upgrade.ActionType.Should().Be(AnalystActionType.Upgrade);
        upgrade.PriorRating.Should().Be("Equal-Weight");
        upgrade.NewRating.Should().Be("Overweight");
        upgrade.PriorTarget.Should().BeNull();
        upgrade.NewTarget.Should().BeNull();
        upgrade.ActionDate.Should().Be(
            DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(1721433600).UtcDateTime));

        actions[1].ActionType.Should().Be(AnalystActionType.Downgrade);
        actions[2].ActionType.Should().Be(AnalystActionType.Initiate);
        actions[2].PriorRating.Should().BeNull("an empty fromGrade must not be fabricated");
    }

    [Fact]
    public void Parse_returns_empty_when_module_absent()
    {
        const string empty = """{ "quoteSummary": { "result": [ { } ], "error": null } }""";

        YahooAnalystActionsSource.Parse(empty, "AAPL").Should().BeEmpty();
    }
}
