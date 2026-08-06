namespace FinanceSentry.Modules.Research.Tests.Retrieval;

using FinanceSentry.Modules.Research.Application.Services;
using FluentAssertions;
using Xunit;

public class ResearchChunkerTests
{
    private static ResearchChunker CreateChunker(int chunkSize = 100, int overlap = 20, int maxChunks = 64)
        => new(RetrievalTestContext.CreateOptions(o =>
        {
            o.ChunkSizeChars = chunkSize;
            o.ChunkOverlapChars = overlap;
            o.MaxChunksPerDocument = maxChunks;
        }));

    [Fact]
    public void Chunk_IsDeterministic_ForSameInput()
    {
        var chunker = CreateChunker();
        var document = RetrievalTestContext.CreateDocument(
            "DRAM pricing", string.Join(" ", Enumerable.Repeat("Contract pricing improved again this quarter.", 20)));

        var first = chunker.Chunk(document);
        var second = chunker.Chunk(document);

        first.Should().HaveCountGreaterThan(1);
        first.Select(c => (c.Ordinal, c.ContentHash, c.StartOffset, c.EndOffset))
            .Should().Equal(second.Select(c => (c.Ordinal, c.ContentHash, c.StartOffset, c.EndOffset)));
    }

    [Fact]
    public void Chunk_AssignsStableSequentialOrdinals()
    {
        var chunker = CreateChunker();
        var document = RetrievalTestContext.CreateDocument(
            "Long doc", string.Join(" ", Enumerable.Repeat("Some sentence about semiconductors.", 30)));

        var chunks = chunker.Chunk(document);

        chunks.Select(c => c.Ordinal).Should().Equal(Enumerable.Range(0, chunks.Count));
        chunks.Should().OnlyContain(c => c.DocumentId == document.Id);
    }

    [Fact]
    public void Chunk_ComputesContentHashOfChunkText()
    {
        var chunker = CreateChunker(chunkSize: 10_000);
        var document = RetrievalTestContext.CreateDocument("Title", "A short body of text.");

        var chunks = chunker.Chunk(document);

        chunks.Should().ContainSingle();
        chunks[0].ContentHash.Should().Be(ResearchChunker.ComputeContentHash("A short body of text."));
        chunks[0].TokenEstimate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Chunk_BreaksAtWhitespace_NotMidWord()
    {
        var chunker = CreateChunker(chunkSize: 50, overlap: 0);
        var document = RetrievalTestContext.CreateDocument(
            "Words", string.Join(" ", Enumerable.Repeat("semiconductor", 20)));

        var chunks = chunker.Chunk(document);

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().OnlyContain(c => c.Text.Split(' ', StringSplitOptions.None)
            .All(w => w == "semiconductor"));
    }

    [Fact]
    public void Chunk_ReturnsEmpty_ForWhitespaceOnlyText()
    {
        var chunker = CreateChunker();
        var document = RetrievalTestContext.CreateDocument("Empty", "   \n\r\n  ");

        chunker.Chunk(document).Should().BeEmpty();
    }

    [Fact]
    public void Chunk_RespectsMaxChunksPerDocument()
    {
        var chunker = CreateChunker(chunkSize: 20, overlap: 0, maxChunks: 3);
        var document = RetrievalTestContext.CreateDocument(
            "Capped", string.Join(" ", Enumerable.Repeat("word", 100)));

        chunker.Chunk(document).Should().HaveCount(3);
    }

    [Fact]
    public void Chunk_NormalizesWindowsLineEndings()
    {
        var chunker = CreateChunker(chunkSize: 10_000);
        var windows = RetrievalTestContext.CreateDocument("T", "line one\r\nline two");
        var unix = RetrievalTestContext.CreateDocument("T", "line one\nline two");

        chunker.Chunk(windows)[0].ContentHash.Should().Be(chunker.Chunk(unix)[0].ContentHash);
    }
}
