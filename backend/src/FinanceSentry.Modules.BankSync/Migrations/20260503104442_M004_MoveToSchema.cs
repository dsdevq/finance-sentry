using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M004_MoveToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bank_sync");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transactions",
                newSchema: "bank_sync");

            migrationBuilder.RenameTable(
                name: "SyncJobs",
                newName: "SyncJobs",
                newSchema: "bank_sync");

            migrationBuilder.RenameTable(
                name: "MonobankCredentials",
                newName: "MonobankCredentials",
                newSchema: "bank_sync");

            migrationBuilder.RenameTable(
                name: "EncryptedCredentials",
                newName: "EncryptedCredentials",
                newSchema: "bank_sync");

            migrationBuilder.RenameTable(
                name: "BankAccounts",
                newName: "BankAccounts",
                newSchema: "bank_sync");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                newName: "audit_logs",
                newSchema: "bank_sync");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "bank_sync",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "SyncJobs",
                schema: "bank_sync",
                newName: "SyncJobs");

            migrationBuilder.RenameTable(
                name: "MonobankCredentials",
                schema: "bank_sync",
                newName: "MonobankCredentials");

            migrationBuilder.RenameTable(
                name: "EncryptedCredentials",
                schema: "bank_sync",
                newName: "EncryptedCredentials");

            migrationBuilder.RenameTable(
                name: "BankAccounts",
                schema: "bank_sync",
                newName: "BankAccounts");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                schema: "bank_sync",
                newName: "audit_logs");
        }
    }
}
