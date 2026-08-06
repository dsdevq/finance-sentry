using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M008_CompanionDataLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThesisIds",
                schema: "research",
                table: "news_articles",
                type: "jsonb",
                nullable: false,
                // Valid empty JSON array — an empty string is not castable to jsonb and would fail the
                // ALTER against existing news_articles rows. Runtime writes come from the list converter.
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "analyst_actions",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Firm = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PriorRating = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    NewRating = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PriorTarget = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    NewTarget = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ActionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analyst_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "analyst_universe_members",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analyst_universe_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "news_sources",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string>(type: "jsonb", nullable: false),
                    ThesisId = table.Column<Guid>(type: "uuid", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastFailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_news_sources_theses_ThesisId",
                        column: x => x.ThesisId,
                        principalSchema: "research",
                        principalTable: "theses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "valuation_snapshots",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TrailingPe = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    ForwardPe = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    EvToEbitda = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    DividendYield = table.Column<decimal>(type: "numeric(8,6)", nullable: true),
                    ConsensusTarget = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    IsStale = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valuation_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_analyst_actions_date",
                schema: "research",
                table: "analyst_actions",
                column: "ActionDate",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_analyst_actions_dedup",
                schema: "research",
                table: "analyst_actions",
                columns: new[] { "Ticker", "Firm", "ActionDate", "ActionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_analyst_actions_ticker_date",
                schema: "research",
                table: "analyst_actions",
                columns: new[] { "Ticker", "ActionDate" });

            migrationBuilder.CreateIndex(
                name: "idx_analyst_universe_ticker",
                schema: "research",
                table: "analyst_universe_members",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_news_sources_thesis",
                schema: "research",
                table: "news_sources",
                column: "ThesisId");

            migrationBuilder.CreateIndex(
                name: "idx_news_sources_url",
                schema: "research",
                table: "news_sources",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_valuation_snapshots_ticker_captured",
                schema: "research",
                table: "valuation_snapshots",
                columns: new[] { "Ticker", "CapturedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analyst_actions",
                schema: "research");

            migrationBuilder.DropTable(
                name: "analyst_universe_members",
                schema: "research");

            migrationBuilder.DropTable(
                name: "news_sources",
                schema: "research");

            migrationBuilder.DropTable(
                name: "valuation_snapshots",
                schema: "research");

            migrationBuilder.DropColumn(
                name: "ThesisIds",
                schema: "research",
                table: "news_articles");
        }
    }
}
