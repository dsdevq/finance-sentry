using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.CryptoSync.Migrations
{
    /// <inheritdoc />
    public partial class M002_MoveToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crypto_sync");

            migrationBuilder.RenameTable(
                name: "CryptoHoldings",
                newName: "CryptoHoldings",
                newSchema: "crypto_sync");

            migrationBuilder.RenameTable(
                name: "BinanceCredentials",
                newName: "BinanceCredentials",
                newSchema: "crypto_sync");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "CryptoHoldings",
                schema: "crypto_sync",
                newName: "CryptoHoldings");

            migrationBuilder.RenameTable(
                name: "BinanceCredentials",
                schema: "crypto_sync",
                newName: "BinanceCredentials");
        }
    }
}
