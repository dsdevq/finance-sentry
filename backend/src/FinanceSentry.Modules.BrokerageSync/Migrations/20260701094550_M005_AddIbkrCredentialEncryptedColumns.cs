using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BrokerageSync.Migrations
{
    /// <inheritdoc />
    public partial class M005_AddIbkrCredentialEncryptedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Under the previous single-tenant model each row only held (UserId,
            // AccountId) with no credentials. Those rows cannot be spawned as
            // per-user IBeam containers, so wipe them here — users will
            // re-connect via the new user/password form. Brokerage holdings are
            // reader-only for this table's semantics so no FK cleanup needed.
            migrationBuilder.Sql("DELETE FROM brokerage_sync.\"IBKRCredentials\";");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedPassword",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedUsername",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "KeyVersion",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "UsernameAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "UsernameIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPassword",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "EncryptedUsername",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "KeyVersion",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "PasswordAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "PasswordIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "UsernameAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "UsernameIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials");
        }
    }
}
