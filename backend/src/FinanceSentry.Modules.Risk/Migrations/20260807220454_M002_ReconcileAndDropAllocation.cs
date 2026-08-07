using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Risk.Migrations
{
    /// <inheritdoc />
    public partial class M002_ReconcileAndDropAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 039: reconcile target allocation into its single home (the IPS) BEFORE dropping the Risk
            // copy. This migration owns the allocation concept (the column it drops) and writes the
            // survivor into the *retained* IPS column, so it is independent of the Research M012
            // migration and safe to apply in either order. Both schemas share one physical database,
            // so the cross-schema SQL runs in a single transaction.
            //
            // Rule (FR-009/FR-010): the IPS wins when it already holds targets (intent is
            // authoritative); otherwise the Risk targets are copied into the IPS, reversibly encoding
            // the symmetric drift band as min/max (fraction -> whole percent) so the drift evaluator
            // recovers the exact band. Neither side populated -> nothing fabricated.

            // (a) FR-011: record Risk allocations discarded because the IPS already holds targets.
            migrationBuilder.Sql(@"
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT rrs.""UserId"" AS user_id, rrs.allocation_targets_json AS discarded
    FROM risk.risk_rule_sets rrs
    JOIN research.investment_policy_statements ips
      ON ips.""UserId"" = rrs.""UserId"" AND ips.""IsCurrent"" = true
    WHERE rrs.""IsCurrent"" = true
      AND rrs.allocation_targets_json IS NOT NULL
      AND jsonb_array_length(rrs.allocation_targets_json) > 0
      AND ips.""AllocationTargets"" IS NOT NULL
      AND jsonb_array_length(ips.""AllocationTargets"") > 0
  LOOP
    RAISE NOTICE '039 reconcile [allocation]: IPS wins for user %; discarding Risk allocation copy %',
      r.user_id, r.discarded;
  END LOOP;
END $$;");

            // (b) Copy Risk targets into the IPS where the IPS has none (one-side-empty -> survives).
            // Guarded on empty IPS targets => idempotent (a second run finds the IPS populated).
            migrationBuilder.Sql(@"
UPDATE research.investment_policy_statements ips
SET ""AllocationTargets"" = sub.translated,
    ""UpdatedAt"" = now()
FROM (
  SELECT rrs.""UserId"" AS user_id,
         jsonb_agg(jsonb_build_object(
           'assetClass', e->>'assetClass',
           'targetPct',  ((e->>'targetPct')::numeric) * 100,
           'minPct',     (((e->>'targetPct')::numeric) - ((e->>'driftBandPct')::numeric)) * 100,
           'maxPct',     (((e->>'targetPct')::numeric) + ((e->>'driftBandPct')::numeric)) * 100
         )) AS translated
  FROM risk.risk_rule_sets rrs
  CROSS JOIN LATERAL jsonb_array_elements(rrs.allocation_targets_json) e
  WHERE rrs.""IsCurrent"" = true
    AND rrs.allocation_targets_json IS NOT NULL
    AND jsonb_array_length(rrs.allocation_targets_json) > 0
  GROUP BY rrs.""UserId""
) sub
WHERE ips.""UserId"" = sub.user_id
  AND ips.""IsCurrent"" = true
  AND (ips.""AllocationTargets"" IS NULL OR jsonb_array_length(ips.""AllocationTargets"") = 0);");

            migrationBuilder.DropColumn(
                name: "allocation_targets_json",
                schema: "risk",
                table: "risk_rule_sets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allocation_targets_json",
                schema: "risk",
                table: "risk_rule_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
