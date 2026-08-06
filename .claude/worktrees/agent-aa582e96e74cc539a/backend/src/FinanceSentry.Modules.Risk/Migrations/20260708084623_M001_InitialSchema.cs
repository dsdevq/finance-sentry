using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Risk.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "risk");

            migrationBuilder.CreateTable(
                name: "holding_snapshots",
                schema: "risk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Sleeve = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,8)", nullable: false),
                    UsdValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holding_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "policy_violation_acks",
                schema: "risk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemediationNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WorseningStepPct = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    ObservedAtAck = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_violation_acks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "risk_rule_sets",
                schema: "risk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPositionWeightPct = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    MaxSleeveWeightPct = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    MinCashBufferPct = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    MaxLossPerThesisPct = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    MaxNewPositionPct = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    TurnoverBudgetPerQuarter = table.Column<int>(type: "integer", nullable: true),
                    allocation_targets_json = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_rule_sets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_holding_snapshots_identity",
                schema: "risk",
                table: "holding_snapshots",
                columns: new[] { "UserId", "Symbol", "Sleeve", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_policy_violation_acks_identity",
                schema: "risk",
                table: "policy_violation_acks",
                columns: new[] { "UserId", "RuleKey", "Subject", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "idx_risk_rule_sets_user_current",
                schema: "risk",
                table: "risk_rule_sets",
                columns: new[] { "UserId", "IsCurrent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holding_snapshots",
                schema: "risk");

            migrationBuilder.DropTable(
                name: "policy_violation_acks",
                schema: "risk");

            migrationBuilder.DropTable(
                name: "risk_rule_sets",
                schema: "risk");
        }
    }
}
