using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BrokerageSync.Migrations
{
    /// <inheritdoc />
    public partial class M003_MoveToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "brokerage_sync");

            migrationBuilder.RenameTable(
                name: "IBKRCredentials",
                newName: "IBKRCredentials",
                newSchema: "brokerage_sync");

            migrationBuilder.RenameTable(
                name: "BrokerageHoldings",
                newName: "BrokerageHoldings",
                newSchema: "brokerage_sync");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "IBKRCredentials",
                schema: "brokerage_sync",
                newName: "IBKRCredentials");

            migrationBuilder.RenameTable(
                name: "BrokerageHoldings",
                schema: "brokerage_sync",
                newName: "BrokerageHoldings");
        }
    }
}
