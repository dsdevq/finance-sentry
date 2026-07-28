namespace FinanceSentry.Modules.Rag.Tests;

using FinanceSentry.Modules.Rag.Domain;
using FluentAssertions;
using Xunit;

public sealed class RagDocumentTests
{
    [Fact]
    public void Create_AssignsNewId()
    {
        var doc = RagDocument.Create(DocType.News, "MU Earnings Beat", DateTimeOffset.UtcNow);

        doc.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_SetsDocType()
    {
        var doc = RagDocument.Create(DocType.Note, "My thesis note", DateTimeOffset.UtcNow);

        doc.DocType.Should().Be(DocType.Note);
    }

    [Fact]
    public void Create_AsOfDateDefaultsToPublishedAt()
    {
        var published = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var doc = RagDocument.Create(DocType.News, "title", published);

        doc.AsOfDate.Should().Be(published);
    }

    [Fact]
    public void Create_AsOfDateCanBeOverridden()
    {
        var published = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var asOf = new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero);
        var doc = RagDocument.Create(DocType.Filing, "10-Q", published, asOfDate: asOf);

        doc.AsOfDate.Should().Be(asOf);
        doc.PublishedAt.Should().Be(published);
    }

    [Fact]
    public void Create_ThrowsOnBlankTitle()
    {
        var act = () => RagDocument.Create(DocType.News, "   ", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsOptionalFields()
    {
        var sourceId = Guid.NewGuid();
        var doc = RagDocument.Create(
            DocType.News, "title", DateTimeOffset.UtcNow,
            sourceId: sourceId, ticker: "MU", url: "https://example.com");

        doc.SourceId.Should().Be(sourceId);
        doc.Ticker.Should().Be("MU");
        doc.Url.Should().Be("https://example.com");
    }
}

public sealed class RagChunkTests
{
    [Fact]
    public void Create_AssignsNewId()
    {
        var chunk = RagChunk.Create(Guid.NewGuid(), "text", 0);

        chunk.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_PreservesOrdinalAndSection()
    {
        var docId = Guid.NewGuid();
        var chunk = RagChunk.Create(docId, "some text", 3, "Risk Factors");

        chunk.DocumentId.Should().Be(docId);
        chunk.Ordinal.Should().Be(3);
        chunk.Section.Should().Be("Risk Factors");
        chunk.ChunkText.Should().Be("some text");
    }

    [Fact]
    public void Create_ThrowsOnEmptyDocumentId()
    {
        var act = () => RagChunk.Create(Guid.Empty, "text", 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ThrowsOnBlankText()
    {
        var act = () => RagChunk.Create(Guid.NewGuid(), "", 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ThrowsOnNegativeOrdinal()
    {
        var act = () => RagChunk.Create(Guid.NewGuid(), "text", -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
