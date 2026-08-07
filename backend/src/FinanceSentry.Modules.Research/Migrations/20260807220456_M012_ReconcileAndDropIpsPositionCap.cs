using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M012_ReconcileAndDropIpsPositionCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 039: reconcile the single-position cap into its single home (the Risk rule set) BEFORE
            // dropping the IPS copy. This migration owns the position-cap concept (the column it drops)
            // and writes the survivor into the *retained* Risk column, so it is independent of the Risk
            // M002 migration and safe to apply in either order. Both schemas share one physical
            // database, so the cross-schema SQL runs in a single transaction.
            //
            // Rule (FR-009): the stricter (lower) cap wins — never loosen a safety limit in a
            // migration. The IPS cap has no unit validation, so it is normalized to the Risk fraction
            // unit first (a value > 1 is a whole percent -> /100). FR-010: a cap present only in the
            // IPS survives (updated into an existing Risk row, or a new Risk row is created); neither
            // side present -> nothing fabricated.

            // (a) FR-011: record the losing cap where both sides hold one and they differ.
            migrationBuilder.Sql(@"
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT ips.""UserId"" AS user_id,
           ips.""MaxSinglePositionPct"" AS ips_raw,
           CASE WHEN ips.""MaxSinglePositionPct"" > 1 THEN ips.""MaxSinglePositionPct"" / 100
                ELSE ips.""MaxSinglePositionPct"" END AS ips_norm,
           rrs.""MaxPositionWeightPct"" AS risk_cap
    FROM research.investment_policy_statements ips
    JOIN risk.risk_rule_sets rrs
      ON rrs.""UserId"" = ips.""UserId"" AND rrs.""IsCurrent"" = true
    WHERE ips.""IsCurrent"" = true
      AND ips.""MaxSinglePositionPct"" IS NOT NULL
      AND rrs.""MaxPositionWeightPct"" IS NOT NULL
      AND (CASE WHEN ips.""MaxSinglePositionPct"" > 1 THEN ips.""MaxSinglePositionPct"" / 100
                ELSE ips.""MaxSinglePositionPct"" END) <> rrs.""MaxPositionWeightPct""
  LOOP
    RAISE NOTICE '039 reconcile [position cap]: user % IPS cap % (normalized %) vs Risk cap %; stricter (lower) wins',
      r.user_id, r.ips_raw, r.ips_norm, r.risk_cap;
  END LOOP;
END $$;");

            // (b) Existing Risk row: survivor = stricter (lower) of the present caps. LEAST ignores
            // NULLs, so an IPS-only or Risk-only cap survives unchanged. Guarded IS DISTINCT FROM =>
            // no write when nothing changes.
            migrationBuilder.Sql(@"
UPDATE risk.risk_rule_sets rrs
SET ""MaxPositionWeightPct"" = LEAST(
      rrs.""MaxPositionWeightPct"",
      CASE WHEN ips.""MaxSinglePositionPct"" > 1 THEN ips.""MaxSinglePositionPct"" / 100
           ELSE ips.""MaxSinglePositionPct"" END)
FROM research.investment_policy_statements ips
WHERE ips.""UserId"" = rrs.""UserId""
  AND ips.""IsCurrent"" = true
  AND rrs.""IsCurrent"" = true
  AND ips.""MaxSinglePositionPct"" IS NOT NULL
  AND LEAST(
        rrs.""MaxPositionWeightPct"",
        CASE WHEN ips.""MaxSinglePositionPct"" > 1 THEN ips.""MaxSinglePositionPct"" / 100
             ELSE ips.""MaxSinglePositionPct"" END)
      IS DISTINCT FROM rrs.""MaxPositionWeightPct"";");

            // (c) FR-010: IPS cap present but the user has no current Risk rule set -> create one so the
            // cap is not lost. Id/CreatedAt use their column defaults.
            migrationBuilder.Sql(@"
INSERT INTO risk.risk_rule_sets (""UserId"", ""Version"", ""IsCurrent"", ""MaxPositionWeightPct"")
SELECT ips.""UserId"", 1, true,
       CASE WHEN ips.""MaxSinglePositionPct"" > 1 THEN ips.""MaxSinglePositionPct"" / 100
            ELSE ips.""MaxSinglePositionPct"" END
FROM research.investment_policy_statements ips
WHERE ips.""IsCurrent"" = true
  AND ips.""MaxSinglePositionPct"" IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM risk.risk_rule_sets rrs
    WHERE rrs.""UserId"" = ips.""UserId"" AND rrs.""IsCurrent"" = true);");

            migrationBuilder.DropColumn(
                name: "MaxSinglePositionPct",
                schema: "research",
                table: "investment_policy_statements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxSinglePositionPct",
                schema: "research",
                table: "investment_policy_statements",
                type: "numeric(6,2)",
                nullable: true);
        }
    }
}
