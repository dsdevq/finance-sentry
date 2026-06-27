using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.CryptoSync.Migrations
{
    /// <inheritdoc />
    public partial class M003_AddCostBasisColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostBasisUsd",
                schema: "crypto_sync",
                table: "CryptoHoldings",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageBuyPriceUsd",
                schema: "crypto_sync",
                table: "CryptoHoldings",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedPnlUsd",
                schema: "crypto_sync",
                table: "CryptoHoldings",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<System.DateTime>(
                name: "LastTradeAt",
                schema: "crypto_sync",
                table: "CryptoHoldings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastTradeId",
                schema: "crypto_sync",
                table: "CryptoHoldings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TradeCount",
                schema: "crypto_sync",
                table: "CryptoHoldings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CostBasisUsd", schema: "crypto_sync", table: "CryptoHoldings");
            migrationBuilder.DropColumn(name: "AverageBuyPriceUsd", schema: "crypto_sync", table: "CryptoHoldings");
            migrationBuilder.DropColumn(name: "RealizedPnlUsd", schema: "crypto_sync", table: "CryptoHoldings");
            migrationBuilder.DropColumn(name: "LastTradeAt", schema: "crypto_sync", table: "CryptoHoldings");
            migrationBuilder.DropColumn(name: "LastTradeId", schema: "crypto_sync", table: "CryptoHoldings");
            migrationBuilder.DropColumn(name: "TradeCount", schema: "crypto_sync", table: "CryptoHoldings");
        }
    }
}
