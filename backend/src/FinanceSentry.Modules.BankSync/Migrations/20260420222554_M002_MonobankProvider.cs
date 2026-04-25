using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M002_MonobankProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlaidItemId",
                table: "BankAccounts",
                newName: "ExternalAccountId");

            migrationBuilder.RenameIndex(
                name: "idx_bank_account_plaid_item_id_unique",
                table: "BankAccounts",
                newName: "idx_bank_account_external_account_id_unique");

            migrationBuilder.AddColumn<Guid>(
                name: "MonobankCredentialId",
                table: "BankAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "BankAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "plaid");

            migrationBuilder.CreateTable(
                name: "MonobankCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    Iv = table.Column<byte[]>(type: "bytea", nullable: false),
                    AuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonobankCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_MonobankCredentialId",
                table: "BankAccounts",
                column: "MonobankCredentialId");

            migrationBuilder.CreateIndex(
                name: "idx_monobank_credential_user_unique",
                table: "MonobankCredentials",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_MonobankCredentials_MonobankCredentialId",
                table: "BankAccounts",
                column: "MonobankCredentialId",
                principalTable: "MonobankCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_MonobankCredentials_MonobankCredentialId",
                table: "BankAccounts");

            migrationBuilder.DropTable(
                name: "MonobankCredentials");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_MonobankCredentialId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "MonobankCredentialId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "BankAccounts");

            migrationBuilder.RenameColumn(
                name: "ExternalAccountId",
                table: "BankAccounts",
                newName: "PlaidItemId");

            migrationBuilder.RenameIndex(
                name: "idx_bank_account_external_account_id_unique",
                table: "BankAccounts",
                newName: "idx_bank_account_plaid_item_id_unique");
        }
    }
}
