using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "research");

            migrationBuilder.CreateTable(
                name: "quote_cache",
                schema: "research",
                columns: table => new
                {
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    PreviousClose = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false, defaultValue: "USD"),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_cache", x => x.Ticker);
                });

            migrationBuilder.CreateTable(
                name: "theses",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ThesisText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    KeyDataPoints = table.Column<string>(type: "jsonb", nullable: false),
                    Catalysts = table.Column<string>(type: "jsonb", nullable: false),
                    InvalidationTriggers = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    BrokenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BrokenReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_theses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "watchlist_items",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Exchange = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_thesis_user_ticker",
                schema: "research",
                table: "theses",
                columns: new[] { "UserId", "Ticker" });

            migrationBuilder.CreateIndex(
                name: "idx_watchlist_user_ticker",
                schema: "research",
                table: "watchlist_items",
                columns: new[] { "UserId", "Ticker" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quote_cache",
                schema: "research");

            migrationBuilder.DropTable(
                name: "theses",
                schema: "research");

            migrationBuilder.DropTable(
                name: "watchlist_items",
                schema: "research");
        }
    }
}
