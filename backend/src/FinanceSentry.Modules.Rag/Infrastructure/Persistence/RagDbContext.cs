namespace FinanceSentry.Modules.Rag.Infrastructure.Persistence;

using FinanceSentry.Modules.Rag.Domain;
using Microsoft.EntityFrameworkCore;

public class RagDbContext(DbContextOptions<RagDbContext> options) : DbContext(options)
{
    public const string Schema = "rag";

    public DbSet<RagDocument> Documents => Set<RagDocument>();
    public DbSet<RagChunk> Chunks => Set<RagChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<RagDocument>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.DocType).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.Title).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2048);
            e.Property(x => x.Ticker).HasMaxLength(20);
            e.Property(x => x.PublishedAt).IsRequired();
            e.Property(x => x.AsOfDate).IsRequired();
            e.Property(x => x.IngestedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

            e.HasIndex(x => new { x.DocType, x.PublishedAt })
                .HasDatabaseName("idx_rag_documents_doctype_published");
            e.HasIndex(x => new { x.Ticker, x.PublishedAt })
                .HasDatabaseName("idx_rag_documents_ticker_published");
            e.HasIndex(x => x.SourceId)
                .HasDatabaseName("idx_rag_documents_source");
        });

        modelBuilder.Entity<RagChunk>(e =>
        {
            e.ToTable("chunks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.DocumentId).IsRequired();
            e.Property(x => x.ChunkText).IsRequired();
            e.Property(x => x.Ordinal).IsRequired();
            e.Property(x => x.Section).HasMaxLength(200);
            e.Property(x => x.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

            e.HasOne<RagDocument>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.DocumentId, x.Ordinal })
                .IsUnique()
                .HasDatabaseName("idx_rag_chunks_doc_ordinal");

            // embedding vector(1024) and content_tsv tsvector are Postgres-specific.
            // They are created via raw SQL in the migration — not mapped as EF properties —
            // so InMemory tests work without the pgvector and tsvector CLR types.
        });
    }
}
