using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M005_ThesisEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "thesis_events",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SubjectPrice = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    BenchmarkPrice = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    BenchmarkTicker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SPY"),
                    PricesPending = table.Column<bool>(type: "boolean", nullable: false),
                    DecisionNote = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thesis_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_thesis_events_pending",
                schema: "research",
                table: "thesis_events",
                columns: new[] { "UserId", "PricesPending" });

            migrationBuilder.CreateIndex(
                name: "idx_thesis_events_subject",
                schema: "research",
                table: "thesis_events",
                columns: new[] { "SubjectType", "SubjectId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "idx_thesis_events_user_type_time",
                schema: "research",
                table: "thesis_events",
                columns: new[] { "UserId", "EventType", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thesis_events",
                schema: "research");
        }
    }
}
