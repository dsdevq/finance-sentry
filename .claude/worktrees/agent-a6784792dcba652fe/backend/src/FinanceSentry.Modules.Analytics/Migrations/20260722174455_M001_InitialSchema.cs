using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Analytics.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "query_audit",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sql = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_query_audit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_query_audit_UserId_CreatedAt",
                schema: "analytics",
                table: "query_audit",
                columns: new[] { "UserId", "CreatedAt" });

            // --- fs_readonly role (feature 033, FR-002) -------------------------------------------
            // SELECT-only, NOLOGIN. The read-only executor connects as the app login and drops into
            // this role via SET LOCAL ROLE, so even a validator bypass has no write path and cannot
            // reach base tables (no USAGE on their schemas). Granted to the current app login so the
            // role switch is permitted.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'fs_readonly') THEN
        CREATE ROLE fs_readonly NOLOGIN;
    END IF;
END
$$;
GRANT USAGE ON SCHEMA analytics TO fs_readonly;
GRANT fs_readonly TO CURRENT_USER;
");

            // --- Curated per-user views (FR-003/FR-004) -------------------------------------------
            // Each is a security_barrier view whose WHERE filters on the transaction-local
            // app.current_user_id GUC; the agent's outer SQL cannot widen it and a missing value
            // fails closed to zero rows. v_analyst_actions is intentionally market-wide (no filter).

            migrationBuilder.Sql(@"
CREATE VIEW analytics.v_transactions WITH (security_barrier = true) AS
SELECT
    COALESCE(t.""PostedDate"", t.""TransactionDate"")::date AS date,
    t.""Amount""          AS amount,
    ba.""Currency""       AS currency,
    t.""MerchantName""    AS merchant,
    t.""MerchantCategory"" AS category,
    ba.""BankName""       AS account_name,
    t.""TransactionType"" AS direction
FROM bank_sync.""Transactions"" t
JOIN bank_sync.""BankAccounts"" ba ON ba.""Id"" = t.""AccountId""
WHERE t.""IsActive""
  AND t.""UserId"" = current_setting('app.current_user_id', true)::uuid;
");

            migrationBuilder.Sql(@"
CREATE VIEW analytics.v_holdings WITH (security_barrier = true) AS
SELECT
    h.""Symbol""         AS symbol,
    h.""InstrumentType"" AS asset_class,
    h.""Quantity""       AS quantity,
    h.""UsdValue""       AS market_value_usd,
    h.""CostBasisUsd""   AS cost_basis_usd,
    h.""Provider""       AS account
FROM brokerage_sync.""BrokerageHoldings"" h
WHERE h.""UserId"" = current_setting('app.current_user_id', true)::uuid
UNION ALL
SELECT
    c.""Asset""                              AS symbol,
    'crypto'                                 AS asset_class,
    (c.""FreeQuantity"" + c.""LockedQuantity"") AS quantity,
    c.""UsdValue""                           AS market_value_usd,
    c.""CostBasisUsd""                       AS cost_basis_usd,
    c.""Provider""                           AS account
FROM crypto_sync.""CryptoHoldings"" c
WHERE c.""UserId"" = current_setting('app.current_user_id', true)::uuid;
");

            migrationBuilder.Sql(@"
CREATE VIEW analytics.v_analyst_actions WITH (security_barrier = true) AS
SELECT
    a.""Ticker""       AS ticker,
    a.""Firm""         AS firm,
    a.""ActionType""   AS action_type,
    a.""PriorRating""  AS prior_rating,
    a.""NewRating""    AS new_rating,
    a.""PriorTarget""  AS prior_target,
    a.""NewTarget""    AS new_target,
    a.""ActionDate""   AS action_date
FROM research.""analyst_actions"" a;
");

            migrationBuilder.Sql(@"
CREATE VIEW analytics.v_net_worth_daily WITH (security_barrier = true) AS
SELECT
    s.""SnapshotDate""   AS as_of_date,
    s.""TotalNetWorth""  AS total_usd,
    s.""BankingTotal""   AS banking_usd,
    s.""BrokerageTotal"" AS brokerage_usd,
    s.""CryptoTotal""    AS crypto_usd
FROM public.""net_worth_snapshots"" s
WHERE s.""UserId"" = current_setting('app.current_user_id', true)::uuid;
");

            migrationBuilder.Sql(@"
CREATE VIEW analytics.v_budgets WITH (security_barrier = true) AS
SELECT
    b.""Category""     AS category,
    'monthly'          AS period,
    b.""MonthlyLimit"" AS limit_amount,
    COALESCE(spend.spent, 0) AS spent_amount,
    (b.""MonthlyLimit"" - COALESCE(spend.spent, 0)) AS remaining
FROM budgets.""budgets"" b
LEFT JOIN LATERAL (
    SELECT SUM(t.""Amount"") AS spent
    FROM bank_sync.""Transactions"" t
    WHERE t.""UserId"" = b.""UserId""
      AND t.""IsActive""
      AND t.""TransactionType"" = 'debit'
      AND t.""MerchantCategory"" = b.""Category""
      AND COALESCE(t.""PostedDate"", t.""TransactionDate"") >= date_trunc('month', CURRENT_DATE)
      AND COALESCE(t.""PostedDate"", t.""TransactionDate"") <  (date_trunc('month', CURRENT_DATE) + INTERVAL '1 month')
) spend ON true
WHERE b.""UserId"" = current_setting('app.current_user_id', true)::uuid;
");

            // --- Grant SELECT on the curated views ONLY (no base-table grants) --------------------
            migrationBuilder.Sql(@"
GRANT SELECT ON
    analytics.v_transactions,
    analytics.v_holdings,
    analytics.v_analyst_actions,
    analytics.v_net_worth_daily,
    analytics.v_budgets
TO fs_readonly;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP VIEW IF EXISTS analytics.v_budgets;
DROP VIEW IF EXISTS analytics.v_net_worth_daily;
DROP VIEW IF EXISTS analytics.v_analyst_actions;
DROP VIEW IF EXISTS analytics.v_holdings;
DROP VIEW IF EXISTS analytics.v_transactions;
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'fs_readonly') THEN
        EXECUTE 'REVOKE fs_readonly FROM ' || quote_ident(CURRENT_USER);
        REVOKE ALL ON SCHEMA analytics FROM fs_readonly;
        DROP ROLE fs_readonly;
    END IF;
END
$$;
");

            migrationBuilder.DropTable(
                name: "query_audit",
                schema: "analytics");
        }
    }
}
