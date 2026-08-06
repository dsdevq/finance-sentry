using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M004_ThesisTriggerV2 : Migration
    {
        // invalidation_triggers is jsonb (System.Text.Json, Web defaults, no enum string converter —
        // ThesisPeriodType serializes as int: Quarter=0, Annual=1). The ThesisInvalidationTrigger
        // shape itself needs no DDL change (R2/data-model.md) — this migration is a pure data
        // backfill to the exact target trigger state for the two seeded theses.
        //
        // DRAM (id 9c091f57-521d-441c-95e1-50400ded1966): backfilled below per the 2026-07-07
        // decision (data-model.md M004 table).
        //
        // TODO(GRAB): data-model.md still carries a placeholder id ("e7b9af2c-…") for the GRAB
        // thesis — tasks.md T001 asks to confirm the real id via
        // `SELECT id, ticker, invalidation_triggers FROM research.theses;` before backfilling it.
        // Intentionally NOT executed here; do not invent the id. Once confirmed, the GRAB backfill
        // is the same UPDATE-by-id shape as DRAM below with:
        //   revenue_yoy proxy GRAB (or null) lessThan 0.10 · 2 quarters;
        //   operating_margin proxy GRAB lessThan 0 · 2 quarters.
        private const string DramThesisId = "9c091f57-521d-441c-95e1-50400ded1966";

        private const string DramTargetTriggers =
            "[" +
            "{\"metric\":\"gross_margin\",\"direction\":\"lessThan\",\"threshold\":0.35,\"proxyTicker\":\"MU\",\"consecutivePeriods\":2,\"periodType\":0}," +
            "{\"metric\":\"revenue_yoy\",\"direction\":\"lessThan\",\"threshold\":0,\"proxyTicker\":\"MU\",\"consecutivePeriods\":2,\"periodType\":0}," +
            "{\"metric\":\"price_drawdown\",\"direction\":\"greaterThan\",\"threshold\":0.30,\"proxyTicker\":null,\"consecutivePeriods\":3,\"periodType\":0}" +
            "]";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: exact-id UPDATE, no-op if the row is absent (e.g. a fresh environment
            // that never seeded the DRAM thesis).
            // Columns are EF-default PascalCase (quoted) — "Id"/"InvalidationTriggers",
            // NOT snake_case. Unquoted identifiers fold to lowercase and fail.
            migrationBuilder.Sql($"""
                UPDATE research.theses
                SET "InvalidationTriggers" = '{DramTargetTriggers}'::jsonb
                WHERE "Id" = '{DramThesisId}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No prior trigger snapshot is captured — Down is intentionally a no-op (data-only
            // migration; reverting would require re-seeding, which is outside migration scope).
        }
    }
}
