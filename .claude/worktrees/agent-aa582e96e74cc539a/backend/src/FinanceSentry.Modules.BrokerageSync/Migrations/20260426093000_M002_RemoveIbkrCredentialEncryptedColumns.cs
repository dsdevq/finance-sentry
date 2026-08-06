using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BrokerageSync.Migrations
{
    /// <summary>
    /// Drops the per-user IBKR credential columns. Under the single-tenant
    /// gateway model the IBeam sidecar owns the IBKR session via
    /// IBKR_ACCOUNT/IBKR_PASSWORD env vars, so credentials no longer flow
    /// through the application.
    /// </summary>
    [DbContext(typeof(BrokerageSyncDbContext))]
    [Migration("20260426093000_M002_RemoveIbkrCredentialEncryptedColumns")]
    public partial class M002_RemoveIbkrCredentialEncryptedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedUsername",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "UsernameIv",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "UsernameAuthTag",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "EncryptedPassword",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "PasswordIv",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "PasswordAuthTag",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "KeyVersion",
                table: "IBKRCredentials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedUsername",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "UsernameIv",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "UsernameAuthTag",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedPassword",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordIv",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordAuthTag",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "KeyVersion",
                table: "IBKRCredentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
