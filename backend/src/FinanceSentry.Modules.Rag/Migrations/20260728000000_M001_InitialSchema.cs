using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Rag.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // pgvector extension — required for vector(1024) column type and HNSW index.
            // Must run before any table creation that references the vector type.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.EnsureSchema(name: "rag");

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "rag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false,
                        defaultValueSql: "gen_random_uuid()"),
                    DocType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AsOfDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"),
                },
                constraints: table => table.PrimaryKey("PK_rag_documents", x => x.Id));

            migrationBuilder.CreateTable(
                name: "chunks",
                schema: "rag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false,
                        defaultValueSql: "gen_random_uuid()"),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkText = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rag_chunks_documents",
                        column: x => x.DocumentId,
                        principalSchema: "rag",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Postgres-specific columns that EF Core does not map as CLR properties.
            migrationBuilder.Sql("""
                ALTER TABLE rag.chunks
                    ADD COLUMN IF NOT EXISTS embedding  vector(1024),
                    ADD COLUMN IF NOT EXISTS content_tsv tsvector
                        GENERATED ALWAYS AS (to_tsvector('english', chunk_text)) STORED;
                """);

            // HNSW index for cosine similarity (ef_search default 40; tune at query time).
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_rag_chunks_embedding
                    ON rag.chunks
                    USING hnsw (embedding vector_cosine_ops)
                    WITH (m = 16, ef_construction = 64);
                """);

            // GIN index for keyword (tsvector) search.
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_rag_chunks_content_tsv
                    ON rag.chunks
                    USING gin (content_tsv);
                """);

            migrationBuilder.CreateIndex(
                name: "idx_rag_documents_doctype_published",
                schema: "rag",
                table: "documents",
                columns: ["DocType", "PublishedAt"]);

            migrationBuilder.CreateIndex(
                name: "idx_rag_documents_ticker_published",
                schema: "rag",
                table: "documents",
                columns: ["Ticker", "PublishedAt"]);

            migrationBuilder.CreateIndex(
                name: "idx_rag_documents_source",
                schema: "rag",
                table: "documents",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "idx_rag_chunks_doc_ordinal",
                schema: "rag",
                table: "chunks",
                columns: ["DocumentId", "Ordinal"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chunks", schema: "rag");
            migrationBuilder.DropTable(name: "documents", schema: "rag");
        }
    }
}
