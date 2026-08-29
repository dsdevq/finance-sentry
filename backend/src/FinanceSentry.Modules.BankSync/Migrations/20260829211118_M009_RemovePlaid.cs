using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M009_RemovePlaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Purge Plaid data before dropping its credential table. Explicit dependency order
            // rather than relying on FK cascades; idempotent when no plaid rows exist.
            migrationBuilder.Sql("""
                DELETE FROM bank_sync."Transactions" t
                USING bank_sync."BankAccounts" a
                WHERE t."AccountId" = a."Id" AND a."Provider" = 'plaid';

                DELETE FROM bank_sync."SyncJobs" sj
                USING bank_sync."BankAccounts" a
                WHERE sj."AccountId" = a."Id" AND a."Provider" = 'plaid';

                DELETE FROM bank_sync."EncryptedCredentials";

                DELETE FROM bank_sync."BankAccounts" WHERE "Provider" = 'plaid';
                """);

            migrationBuilder.DropTable(
                name: "EncryptedCredentials",
                schema: "bank_sync");

            migrationBuilder.DropColumn(
                name: "WebhookTriggered",
                schema: "bank_sync",
                table: "SyncJobs");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                schema: "bank_sync",
                table: "BankAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "plaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WebhookTriggered",
                schema: "bank_sync",
                table: "SyncJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                schema: "bank_sync",
                table: "BankAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "plaid",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "EncryptedCredentials",
                schema: "bank_sync",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EncryptedData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Iv = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlaidSyncCursor = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptedCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncryptedCredentials_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "bank_sync",
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_encrypted_credential_account_id_unique",
                schema: "bank_sync",
                table: "EncryptedCredentials",
                column: "AccountId",
                unique: true);
        }
    }
}
