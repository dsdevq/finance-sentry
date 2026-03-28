using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <summary>
    /// M002: Add soft-delete support to Transactions table.
    ///
    /// Required by T309-A (DELETE /accounts/{id} soft-deletes associated transactions)
    /// and T527 (DataRetentionJob archives transactions older than 24 months).
    ///
    /// Adds: IsActive (default true), DeletedAt (nullable), ArchivedReason (nullable),
    ///       UserId (denormalized FK for query performance),
    ///       MerchantCategory (for Phase 5 spending statistics).
    /// </summary>
    public partial class M002_TransactionSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add soft-delete columns
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedReason",
                table: "Transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Add denormalized UserId FK for efficient user-scoped queries (avoids JOIN to BankAccounts)
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            // Add MerchantCategory for Phase 5 spending statistics
            migrationBuilder.AddColumn<string>(
                name: "MerchantCategory",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Index: partial index on active transactions per account (covers soft-delete filter)
            migrationBuilder.CreateIndex(
                name: "idx_transaction_account_active",
                table: "Transactions",
                columns: new[] { "AccountId", "IsActive" });

            // Index: user + active for cross-account queries (aggregation in Phase 5)
            migrationBuilder.CreateIndex(
                name: "idx_transaction_user_active",
                table: "Transactions",
                columns: new[] { "UserId", "IsActive" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "idx_transaction_account_active", table: "Transactions");
            migrationBuilder.DropIndex(name: "idx_transaction_user_active", table: "Transactions");

            migrationBuilder.DropColumn(name: "IsActive", table: "Transactions");
            migrationBuilder.DropColumn(name: "DeletedAt", table: "Transactions");
            migrationBuilder.DropColumn(name: "ArchivedReason", table: "Transactions");
            migrationBuilder.DropColumn(name: "UserId", table: "Transactions");
            migrationBuilder.DropColumn(name: "MerchantCategory", table: "Transactions");
        }
    }
}
