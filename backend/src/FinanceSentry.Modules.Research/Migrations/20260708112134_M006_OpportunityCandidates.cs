using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M006_OpportunityCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "candidate_scores",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    StructureScore = table.Column<int>(type: "integer", nullable: true),
                    FundamentalsScore = table.Column<int>(type: "integer", nullable: true),
                    CrowdingClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IpsFit = table.Column<string>(type: "jsonb", nullable: false),
                    Evidence = table.Column<string>(type: "jsonb", nullable: false),
                    FormulaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_candidates",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PromotedThesisId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NominationReasons = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_candidates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_candidate_scores_candidate_scored",
                schema: "research",
                table: "candidate_scores",
                columns: new[] { "CandidateId", "ScoredAt" });

            migrationBuilder.CreateIndex(
                name: "idx_opportunity_candidates_user_status",
                schema: "research",
                table: "opportunity_candidates",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_opportunity_candidates_user_ticker",
                schema: "research",
                table: "opportunity_candidates",
                columns: new[] { "UserId", "Ticker" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_scores",
                schema: "research");

            migrationBuilder.DropTable(
                name: "opportunity_candidates",
                schema: "research");
        }
    }
}
