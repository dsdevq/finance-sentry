using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M003_InvestmentPolicyStatement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "investment_policy_statements",
                schema: "research",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    Goals = table.Column<string>(type: "jsonb", nullable: false),
                    PrimaryHorizonYears = table.Column<int>(type: "integer", nullable: false),
                    EmergencyCushionUsd = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    RiskTolerance = table.Column<int>(type: "integer", nullable: false),
                    RiskCapacity = table.Column<int>(type: "integer", nullable: true),
                    MaxDrawdownTolerancePct = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    AllocationTargets = table.Column<string>(type: "jsonb", nullable: false),
                    RebalancingRule = table.Column<string>(type: "jsonb", nullable: false),
                    ContributionPlan = table.Column<string>(type: "jsonb", nullable: true),
                    SellDiscipline = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CoolingOffDays = table.Column<int>(type: "integer", nullable: false),
                    Exclusions = table.Column<string>(type: "jsonb", nullable: false),
                    MaxSinglePositionPct = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ReviewCadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investment_policy_statements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_ips_user_current",
                schema: "research",
                table: "investment_policy_statements",
                columns: new[] { "UserId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "idx_ips_user_version",
                schema: "research",
                table: "investment_policy_statements",
                columns: new[] { "UserId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investment_policy_statements",
                schema: "research");
        }
    }
}
