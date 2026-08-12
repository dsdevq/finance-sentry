using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M008_AccountSyncWatermark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastTransactionSyncAt",
                schema: "bank_sync",
                table: "BankAccounts",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: accounts that already hold transactions resume from their latest row so
            // they don't re-run the initial history import. The 2026-07-04 floor matters: the
            // dedup HMAC key was rotated on 2026-07-01 (#220), so re-fetching a window that
            // overlaps rows stored under the old key would re-ingest every one of them as a
            // duplicate (their stored hashes can no longer match). Accounts with no rows keep
            // NULL and run the initial import — nothing exists there to duplicate.
            migrationBuilder.Sql("""
                UPDATE bank_sync."BankAccounts" a
                SET "LastTransactionSyncAt" = GREATEST(t.max_date, TIMESTAMPTZ '2026-07-04 00:00:00+00')
                FROM (
                    SELECT "AccountId", MAX(COALESCE("PostedDate", "TransactionDate")) AS max_date
                    FROM bank_sync."Transactions"
                    WHERE "IsActive" = true
                    GROUP BY "AccountId"
                ) t
                WHERE t."AccountId" = a."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTransactionSyncAt",
                schema: "bank_sync",
                table: "BankAccounts");
        }
    }
}
