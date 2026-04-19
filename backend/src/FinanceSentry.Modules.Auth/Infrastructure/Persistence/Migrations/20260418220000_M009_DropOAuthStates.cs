using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Auth.Infrastructure.Persistence.Migrations;

public partial class M009_DropOAuthStates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_OAuthStates_State", "OAuthStates");
        migrationBuilder.DropIndex("IX_OAuthStates_ExpiresAt", "OAuthStates");
        migrationBuilder.DropTable("OAuthStates");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OAuthStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OAuthStates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OAuthStates_ExpiresAt",
            table: "OAuthStates",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_OAuthStates_State",
            table: "OAuthStates",
            column: "State",
            unique: true);
    }
}
