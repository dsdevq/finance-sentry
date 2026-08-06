using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M010_RecommendationTrends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recommendation_trends",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    StrongBuy = table.Column<int>(type: "integer", nullable: false),
                    Buy = table.Column<int>(type: "integer", nullable: false),
                    Hold = table.Column<int>(type: "integer", nullable: false),
                    Sell = table.Column<int>(type: "integer", nullable: false),
                    StrongSell = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendation_trends", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_recommendation_trends_ticker_period",
                schema: "research",
                table: "recommendation_trends",
                columns: new[] { "Ticker", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recommendation_trends",
                schema: "research");
        }
    }
}
