using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Subscriptions.Migrations
{
    /// <inheritdoc />
    public partial class M002_AddKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "detected_subscriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "subscription");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "detected_subscriptions");
        }
    }
}
