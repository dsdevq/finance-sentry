using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BrokerageSync.Migrations
{
    /// <inheritdoc />
    public partial class M004_AddBrokerageCostBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageCostUsd",
                schema: "brokerage_sync",
                table: "BrokerageHoldings",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostBasisUsd",
                schema: "brokerage_sync",
                table: "BrokerageHoldings",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<System.DateTime>(
                name: "AcquiredAt",
                schema: "brokerage_sync",
                table: "BrokerageHoldings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AverageCostUsd", schema: "brokerage_sync", table: "BrokerageHoldings");
            migrationBuilder.DropColumn(name: "CostBasisUsd", schema: "brokerage_sync", table: "BrokerageHoldings");
            migrationBuilder.DropColumn(name: "AcquiredAt", schema: "brokerage_sync", table: "BrokerageHoldings");
        }
    }
}
