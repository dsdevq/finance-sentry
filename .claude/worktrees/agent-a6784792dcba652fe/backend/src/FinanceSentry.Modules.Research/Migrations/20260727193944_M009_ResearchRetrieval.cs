using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M009_ResearchRetrieval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "research_documents",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "text", nullable: true),
                    SourceName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Tickers = table.Column<string>(type: "jsonb", nullable: false),
                    ThesisIds = table.Column<string>(type: "jsonb", nullable: false),
                    IndexStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IndexFailureReason = table.Column<string>(type: "text", nullable: true),
                    IndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "research_chunks",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    StartOffset = table.Column<int>(type: "integer", nullable: true),
                    EndOffset = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_research_chunks_research_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "research",
                        principalTable: "research_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "research_embeddings",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Dimensions = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingVersion = table.Column<int>(type: "integer", nullable: false),
                    Vector = table.Column<float[]>(type: "real[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_research_embeddings_research_chunks_ChunkId",
                        column: x => x.ChunkId,
                        principalSchema: "research",
                        principalTable: "research_chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_research_chunks_doc_ordinal_hash",
                schema: "research",
                table: "research_chunks",
                columns: new[] { "DocumentId", "Ordinal", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_research_documents_published",
                schema: "research",
                table: "research_documents",
                column: "PublishedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_research_documents_source_identity",
                schema: "research",
                table: "research_documents",
                columns: new[] { "SourceType", "SourceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_research_documents_status_captured",
                schema: "research",
                table: "research_documents",
                columns: new[] { "IndexStatus", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_research_embeddings_chunk_provider_model_version",
                schema: "research",
                table: "research_embeddings",
                columns: new[] { "ChunkId", "Provider", "Model", "EmbeddingVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "research_embeddings",
                schema: "research");

            migrationBuilder.DropTable(
                name: "research_chunks",
                schema: "research");

            migrationBuilder.DropTable(
                name: "research_documents",
                schema: "research");
        }
    }
}
