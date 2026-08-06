using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Budgets.Migrations
{
    /// <inheritdoc />
    public partial class M002_MoveToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budgets");

            migrationBuilder.RenameTable(
                name: "budgets",
                newName: "budgets",
                newSchema: "budgets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "budgets",
                schema: "budgets",
                newName: "budgets");
        }
    }
}
