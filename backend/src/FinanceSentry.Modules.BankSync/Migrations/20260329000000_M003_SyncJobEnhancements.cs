using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <summary>
    /// M003: Add Phase 4 enhancements to the SyncJobs table.
    ///
    /// New columns:
    ///   UserId                 — denormalised FK to User for query performance
    ///   CorrelationId          — distributed tracing ID (nullable)
    ///   TransactionCountFetched — total transactions returned by Plaid
    ///   TransactionCountDeduped — new transactions actually persisted after dedup
    ///   RetryCount             — how many Hangfire/Polly retry attempts
    ///   WebhookTriggered       — whether the sync was triggered by a Plaid webhook
    ///
    /// New index:
    ///   idx_sync_job_account_status — composite on (AccountId, Status) for fast "is running?" check
    /// </summary>
    public partial class M003_SyncJobEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SyncJobs",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "SyncJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCountFetched",
                table: "SyncJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCountDeduped",
                table: "SyncJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "SyncJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WebhookTriggered",
                table: "SyncJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "idx_sync_job_account_status",
                table: "SyncJobs",
                columns: new[] { "AccountId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_sync_job_account_status",
                table: "SyncJobs");

            migrationBuilder.DropColumn(name: "UserId",                  table: "SyncJobs");
            migrationBuilder.DropColumn(name: "CorrelationId",           table: "SyncJobs");
            migrationBuilder.DropColumn(name: "TransactionCountFetched", table: "SyncJobs");
            migrationBuilder.DropColumn(name: "TransactionCountDeduped", table: "SyncJobs");
            migrationBuilder.DropColumn(name: "RetryCount",              table: "SyncJobs");
            migrationBuilder.DropColumn(name: "WebhookTriggered",        table: "SyncJobs");
        }
    }
}
