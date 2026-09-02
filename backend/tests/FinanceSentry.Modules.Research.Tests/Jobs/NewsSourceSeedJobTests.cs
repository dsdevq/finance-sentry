namespace FinanceSentry.Modules.Research.Tests.Jobs;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Infrastructure.Jobs;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using FinanceSentry.Modules.Research.Tests.Companion;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Seed + self-repair behaviour of <see cref="NewsSourceSeedJob"/> (spec 046, issue #318). The repair
/// path exists because idempotency-by-URL cannot fix a row whose URL is the thing that is wrong: the
/// deployed TrendForce source sat on the marketing hub for weeks after #326 corrected the constant.
/// </summary>
public sealed class NewsSourceSeedJobTests : IDisposable
{
    private const string LegacyUrl = "https://www.trendforce.com/presscenter/";
    private const string CanonicalUrl = "https://www.trendforce.com/presscenter/news";

    private readonly ResearchDbContext _db = CompanionTestContext.Create();
    private readonly FakeNewsSourceRepository _sources = new();

    private NewsSourceSeedJob Job =>
        new(_sources, _db, NullLogger<NewsSourceSeedJob>.Instance);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Legacy_trendforce_row_is_repointed_in_place_keeping_its_identity()
    {
        var thesisId = await SeedDramThesisAsync();
        var legacy = AddLegacySource(thesisId, consecutiveFailures: 17, enabled: false);

        await Job.ExecuteAsync();

        var trendForce = _sources.Sources.Where(s => s.Name == "TrendForce Press Center").ToList();
        trendForce.Should().ContainSingle("the row is repaired, not replaced");
        trendForce[0].Id.Should().Be(legacy.Id);
        trendForce[0].Url.Should().Be(CanonicalUrl);
        trendForce[0].ThesisId.Should().Be(thesisId);
        trendForce[0].Keywords.Should().Equal("DRAM", "HBM", "NAND", "memory");
    }

    [Fact]
    public async Task Repair_clears_the_failure_state_that_had_retired_the_row()
    {
        await SeedDramThesisAsync();
        AddLegacySource(consecutiveFailures: 17, enabled: false);

        await Job.ExecuteAsync();

        var repaired = _sources.Sources.Single(s => s.Name == "TrendForce Press Center");
        repaired.Enabled.Should().BeTrue("a row whose cause of failure was just removed must not stay retired");
        repaired.ConsecutiveFailures.Should().Be(0);
        repaired.LastFailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Repair_is_idempotent_across_runs()
    {
        await SeedDramThesisAsync();
        AddLegacySource(consecutiveFailures: 17, enabled: false);

        await Job.ExecuteAsync();
        await Job.ExecuteAsync();

        _sources.Sources.Where(s => s.Name == "TrendForce Press Center").Should().ContainSingle();
    }

    [Fact]
    public async Task Legacy_row_is_dropped_when_a_canonical_row_already_exists()
    {
        var thesisId = await SeedDramThesisAsync();
        AddLegacySource(thesisId, consecutiveFailures: 17, enabled: false);
        var canonical = new NewsSource
        {
            Name = "TrendForce Press Center",
            Kind = NewsSourceKind.Page,
            Url = CanonicalUrl,
            ThesisId = thesisId,
            Keywords = ["DRAM"],
        };
        _sources.Sources.Add(canonical);

        await Job.ExecuteAsync();

        _sources.Sources.Should().NotContain(s => s.Url == LegacyUrl, "its URL is taken; it cannot be rewritten");
        var survivor = _sources.Sources.Single(s => s.Name == "TrendForce Press Center");
        survivor.Id.Should().Be(canonical.Id, "the row that has been ingesting is the one that survives");
    }

    [Fact]
    public async Task Repair_leaves_unrelated_sources_untouched()
    {
        await SeedDramThesisAsync();
        var unrelated = new NewsSource
        {
            Name = "Denys's own TrendForce filter",
            Kind = NewsSourceKind.Page,
            Url = "https://www.trendforce.com/presscenter/news/Semiconductors",
            Enabled = false,
            ConsecutiveFailures = 4,
            LastFailureReason = "boom",
        };
        _sources.Sources.Add(unrelated);

        await Job.ExecuteAsync();

        var after = _sources.Sources.Single(s => s.Id == unrelated.Id);
        after.Url.Should().Be("https://www.trendforce.com/presscenter/news/Semiconductors");
        after.Enabled.Should().BeFalse("only URLs this job itself shipped wrong get rewritten");
        after.ConsecutiveFailures.Should().Be(4);
    }

    [Fact]
    public async Task Fresh_install_seeds_the_canonical_url_and_the_market_wide_feeds()
    {
        var thesisId = await SeedDramThesisAsync();

        await Job.ExecuteAsync();

        var trendForce = _sources.Sources.Single(s => s.Name == "TrendForce Press Center");
        trendForce.Url.Should().Be(CanonicalUrl);
        trendForce.ThesisId.Should().Be(thesisId);
        _sources.Sources.Should().Contain(s => s.Url == "https://finance.yahoo.com/news/rssindex");
    }

    [Fact]
    public async Task Legacy_row_is_still_repaired_when_no_dram_thesis_exists()
    {
        // The insert path is gated on a DRAM thesis; the repair must not inherit that gate, or a row
        // broken before the thesis was filed stays broken forever.
        AddLegacySource(consecutiveFailures: 17, enabled: false);

        await Job.ExecuteAsync();

        _sources.Sources.Single(s => s.Name == "TrendForce Press Center").Url.Should().Be(CanonicalUrl);
    }

    private NewsSource AddLegacySource(Guid? thesisId = null, int consecutiveFailures = 0, bool enabled = true)
    {
        var legacy = new NewsSource
        {
            Name = "TrendForce Press Center",
            Kind = NewsSourceKind.Page,
            Url = LegacyUrl,
            ThesisId = thesisId,
            Keywords = ["DRAM", "HBM", "NAND", "memory"],
            Enabled = enabled,
            ConsecutiveFailures = consecutiveFailures,
            LastFailureReason = "TrendForce press-center article list not found",
        };
        _sources.Sources.Add(legacy);
        return legacy;
    }

    private async Task<Guid> SeedDramThesisAsync()
    {
        var thesis = new InvestmentThesis
        {
            UserId = Guid.NewGuid(),
            Ticker = "DRAM",
            ThesisText = "Memory supply stays tight through 2027.",
        };
        _db.Theses.Add(thesis);
        await _db.SaveChangesAsync();
        return thesis.Id;
    }
}
