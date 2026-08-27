using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M013_ThesisEntryPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EntryPrice",
                schema: "research",
                table: "theses",
                type: "numeric(18,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryPrice",
                schema: "research",
                table: "theses");
        }
    }
}
