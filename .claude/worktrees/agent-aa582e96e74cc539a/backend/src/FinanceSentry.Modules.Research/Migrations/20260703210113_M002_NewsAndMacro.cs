using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M002_NewsAndMacro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "macro_events",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Event = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Importance = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_macro_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "news_articles",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Tickers = table.Column<string>(type: "jsonb", nullable: false),
                    Categories = table.Column<string>(type: "jsonb", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_articles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_macro_date",
                schema: "research",
                table: "macro_events",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "idx_macro_dedup",
                schema: "research",
                table: "macro_events",
                columns: new[] { "EventDate", "Region", "Event" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_news_hash",
                schema: "research",
                table: "news_articles",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_news_published",
                schema: "research",
                table: "news_articles",
                column: "PublishedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "macro_events",
                schema: "research");

            migrationBuilder.DropTable(
                name: "news_articles",
                schema: "research");
        }
    }
}
