namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FluentAssertions;
using Xunit;

/// <summary>
/// Thesis/keyword tagging rules (feature 030, T039/FR-008): a thesis-registered source tags its
/// articles, gated by any keyword filter; a market-wide source tags nothing; keywords never drop an
/// article (breadth preserved).
/// </summary>
public sealed class NewsSourceTaggingTests
{
    private static readonly Guid DramThesis = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Thesis_source_with_no_keywords_tags_every_article()
    {
        var source = new NewsSource { ThesisId = DramThesis, Keywords = [] };

        NewsSourceTagging.ResolveThesisIds(source, "Anything at all", null)
            .Should().ContainSingle().Which.Should().Be(DramThesis);
    }

    [Fact]
    public void Thesis_source_tags_when_a_keyword_matches_title_or_summary()
    {
        var source = new NewsSource { ThesisId = DramThesis, Keywords = ["DRAM", "HBM"] };

        NewsSourceTagging.ResolveThesisIds(source, "Micron guides HBM higher", null)
            .Should().ContainSingle().Which.Should().Be(DramThesis);

        NewsSourceTagging.ResolveThesisIds(source, "Unrelated headline", "mentions dram pricing")
            .Should().ContainSingle("keyword match is case-insensitive and checks the summary too");
    }

    [Fact]
    public void Thesis_source_does_not_tag_when_no_keyword_matches()
    {
        var source = new NewsSource { ThesisId = DramThesis, Keywords = ["DRAM", "HBM"] };

        NewsSourceTagging.ResolveThesisIds(source, "Oil prices slump", "crude inventory build")
            .Should().BeEmpty();
    }

    [Fact]
    public void Market_wide_source_tags_no_thesis()
    {
        var source = new NewsSource { ThesisId = null, Keywords = [] };

        NewsSourceTagging.ResolveThesisIds(source, "DRAM prices rise", null).Should().BeEmpty();
    }
}
