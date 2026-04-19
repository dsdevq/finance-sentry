using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaidItemId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountNumberLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    CurrentBalance = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    SyncStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncryptedCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Iv = table.Column<byte[]>(type: "bytea", nullable: false),
                    AuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlaidSyncCursor = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptedCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncryptedCredentials_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionsSynced = table.Column<int>(type: "integer", nullable: false),
                    TransactionCountFetched = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TransactionCountDeduped = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastTransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    WebhookTriggered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncJobs_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UniqueHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsPending = table.Column<bool>(type: "boolean", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MerchantName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MerchantCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_resource",
                table: "audit_logs",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_user_performed_at",
                table: "audit_logs",
                columns: new[] { "UserId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_bank_account_plaid_item_id_unique",
                table: "BankAccounts",
                column: "PlaidItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_bank_account_sync_status",
                table: "BankAccounts",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "idx_bank_account_user_id",
                table: "BankAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_encrypted_credential_account_id_unique",
                table: "EncryptedCredentials",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sync_job_account_id",
                table: "SyncJobs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "idx_sync_job_account_status",
                table: "SyncJobs",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_sync_job_created_at",
                table: "SyncJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_sync_job_status",
                table: "SyncJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_account_active",
                table: "Transactions",
                columns: new[] { "AccountId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "idx_transaction_account_id",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_account_unique_hash_unique",
                table: "Transactions",
                columns: new[] { "AccountId", "UniqueHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_transaction_created_at",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_posted_date",
                table: "Transactions",
                column: "PostedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "EncryptedCredentials");

            migrationBuilder.DropTable(
                name: "SyncJobs");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "BankAccounts");
        }
    }
}
