using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Alerts.Migrations
{
    /// <inheritdoc />
    public partial class M002_MoveToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "alerts");

            migrationBuilder.RenameTable(
                name: "alerts",
                newName: "alerts",
                newSchema: "alerts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "alerts",
                schema: "alerts",
                newName: "alerts");
        }
    }
}
