using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M007_PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchivedReason",
                table: "Transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantCategory",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "BankAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.AuditId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_transaction_account_active",
                table: "Transactions",
                columns: new[] { "AccountId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_resource",
                table: "audit_logs",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_user_performed_at",
                table: "audit_logs",
                columns: new[] { "UserId", "PerformedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropIndex(
                name: "idx_transaction_account_active",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ArchivedReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MerchantCategory",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "BankAccounts");
        }
    }
}
