using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <summary>
    /// Data repair: the MarketBeat parser concatenated the "Subscribe to MarketBeat All Access…"
    /// promo text onto every ingested <c>Firm</c> value. <c>Firm</c> is part of the unique index
    /// <c>idx_analyst_actions_dedup (Ticker, Firm, ActionDate, ActionType)</c>, so re-ingestion
    /// cannot heal the rows — this migration strips the promo suffix in place. Where a cleaned
    /// name would collide on the unique index (a clean row already exists, or two polluted rows
    /// clean to the same key) the richer/older row wins and the other is deleted first.
    /// </summary>
    public partial class M011_CleanAnalystActionFirms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — dedupe before cleaning: delete every polluted row whose cleaned key would
            // violate idx_analyst_actions_dedup. Among polluted twins, keep the row with the most
            // populated rating/target fields, then the oldest IngestedAt. A pre-existing clean row
            // (ingested by the fixed parser) always wins over its polluted duplicate.
            migrationBuilder.Sql("""
                WITH cleaned AS (
                    SELECT "Id", "Ticker", "ActionDate", "ActionType", "IngestedAt",
                           btrim(left("Firm", strpos("Firm", 'Subscribe to MarketBeat All Access') - 1)) AS clean_firm,
                           (("PriorRating" IS NOT NULL)::int + ("NewRating" IS NOT NULL)::int +
                            ("PriorTarget" IS NOT NULL)::int + ("NewTarget" IS NOT NULL)::int) AS richness
                    FROM research.analyst_actions
                    WHERE strpos("Firm", 'Subscribe to MarketBeat All Access') > 1
                ),
                ranked AS (
                    SELECT "Id",
                           row_number() OVER (
                               PARTITION BY "Ticker", clean_firm, "ActionDate", "ActionType"
                               ORDER BY richness DESC, "IngestedAt" ASC, "Id" ASC) AS rn
                    FROM cleaned
                ),
                losers AS (
                    SELECT "Id" FROM ranked WHERE rn > 1
                    UNION
                    SELECT c."Id"
                    FROM cleaned c
                    JOIN research.analyst_actions existing
                      ON existing."Ticker" = c."Ticker"
                     AND existing."Firm" = c.clean_firm
                     AND existing."ActionDate" = c."ActionDate"
                     AND existing."ActionType" = c."ActionType"
                )
                DELETE FROM research.analyst_actions aa
                USING losers l
                WHERE aa."Id" = l."Id";
                """);

            // Step 2 — strip the promo suffix. The strpos > 1 guard skips (hypothetical) rows that
            // are promo-only, so Firm can never end up empty.
            migrationBuilder.Sql("""
                UPDATE research.analyst_actions
                SET "Firm" = btrim(left("Firm", strpos("Firm", 'Subscribe to MarketBeat All Access') - 1))
                WHERE strpos("Firm", 'Subscribe to MarketBeat All Access') > 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data repair — the deleted duplicate rows and stripped promo suffixes
            // cannot be reconstructed. Down is intentionally a no-op.
        }
    }
}
