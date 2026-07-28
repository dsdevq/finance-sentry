namespace FinanceSentry.Modules.Rag.Tests;

using FinanceSentry.Modules.Rag.Domain;
using FinanceSentry.Modules.Rag.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests the EF CRUD operations of <see cref="EfCorpusRepository"/> against an InMemory database.
/// <see cref="ICorpusRepository.SearchAsync"/> is Postgres-specific (pgvector + tsvector SQL)
/// and is covered through the <see cref="ICorpusRepository"/> interface mock in higher-level tests.
/// </summary>
public sealed class EfCorpusRepositoryTests
{
    [Fact]
    public async Task AddDocumentAsync_PersistsDocument()
    {
        await using var db = TestSupport.NewContext();
        var repo = new EfCorpusRepository(db);

        var doc = RagDocument.Create(DocType.News, "MU Q3 Earnings Beat", DateTimeOffset.UtcNow, ticker: "MU");
        await repo.AddDocumentAsync(doc);
        await repo.SaveChangesAsync();

        var stored = await db.Documents.FindAsync(doc.Id);
        stored.Should().NotBeNull();
        stored!.Title.Should().Be("MU Q3 Earnings Beat");
        stored.Ticker.Should().Be("MU");
        stored.DocType.Should().Be(DocType.News);
    }

    [Fact]
    public async Task AddChunksAsync_PersistsAllChunks()
    {
        await using var db = TestSupport.NewContext();
        var repo = new EfCorpusRepository(db);

        var doc = RagDocument.Create(DocType.Note, "My MU thesis", DateTimeOffset.UtcNow);
        await repo.AddDocumentAsync(doc);

        var chunks = new[]
        {
            RagChunk.Create(doc.Id, "MU is well positioned in the DRAM upcycle.", 0),
            RagChunk.Create(doc.Id, "Supply tightening expected through H2 2026.", 1),
        };
        await repo.AddChunksAsync(chunks);
        await repo.SaveChangesAsync();

        var storedChunks = db.Chunks.Where(c => c.DocumentId == doc.Id).ToList();
        storedChunks.Should().HaveCount(2);
        storedChunks.Select(c => c.Ordinal).Should().BeEquivalentTo([0, 1]);
    }

    [Fact]
    public async Task AddDocument_ThenAddChunks_ForeignKeyHeld()
    {
        await using var db = TestSupport.NewContext();
        var repo = new EfCorpusRepository(db);

        var doc = RagDocument.Create(DocType.Filing, "MU 10-Q 2026 Q3",
            new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero),
            ticker: "MU",
            url: "https://sec.gov/filing/mu-10q");
        await repo.AddDocumentAsync(doc);

        var chunk = RagChunk.Create(doc.Id, "Revenue increased 18% YoY.", 0, "Financial Statements");
        await repo.AddChunksAsync([chunk]);
        await repo.SaveChangesAsync();

        var storedChunk = db.Chunks.First(c => c.DocumentId == doc.Id);
        storedChunk.Section.Should().Be("Financial Statements");
        storedChunk.ChunkText.Should().Contain("Revenue");
    }

    [Fact]
    public async Task AddDocument_DifferentDocTypes_StoredCorrectly()
    {
        await using var db = TestSupport.NewContext();
        var repo = new EfCorpusRepository(db);

        var news = RagDocument.Create(DocType.News, "DRAM prices surge", DateTimeOffset.UtcNow);
        var note = RagDocument.Create(DocType.Note, "Personal investment note", DateTimeOffset.UtcNow);
        await repo.AddDocumentAsync(news);
        await repo.AddDocumentAsync(note);
        await repo.SaveChangesAsync();

        db.Documents.Should().HaveCount(2);
        db.Documents.Count(d => d.DocType == DocType.News).Should().Be(1);
        db.Documents.Count(d => d.DocType == DocType.Note).Should().Be(1);
    }
}
