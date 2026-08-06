using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BrokerageSync.Migrations
{
    /// <inheritdoc />
    public partial class M006_IbkrOAuthArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The previous rows hold username/password material that has no
            // meaning under the OAuth 1.0a model. Reusing the byte columns via
            // rename would leave them mislabeled with empty OAuth identifiers, so
            // wipe them — users re-connect via the new self-service OAuth form.
            migrationBuilder.Sql("DELETE FROM brokerage_sync.\"IBKRCredentials\";");

            migrationBuilder.RenameColumn(
                name: "UsernameIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "SignatureKeyIv");

            migrationBuilder.RenameColumn(
                name: "UsernameAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "SignatureKeyAuthTag");

            migrationBuilder.RenameColumn(
                name: "PasswordIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "EncryptionKeyIv");

            migrationBuilder.RenameColumn(
                name: "PasswordAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "EncryptionKeyAuthTag");

            migrationBuilder.RenameColumn(
                name: "EncryptedUsername",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "EncryptedSignatureKey");

            migrationBuilder.RenameColumn(
                name: "EncryptedPassword",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "EncryptedEncryptionKey");

            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "AccessTokenSecretAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "AccessTokenSecretIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ConsumerKey",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DhParam",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedAccessTokenSecret",
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
                name: "AccessToken",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "AccessTokenSecretAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "AccessTokenSecretIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "ConsumerKey",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "DhParam",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.DropColumn(
                name: "EncryptedAccessTokenSecret",
                schema: "brokerage_sync",
                table: "IBKRCredentials");

            migrationBuilder.RenameColumn(
                name: "SignatureKeyIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "UsernameIv");

            migrationBuilder.RenameColumn(
                name: "SignatureKeyAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "UsernameAuthTag");

            migrationBuilder.RenameColumn(
                name: "EncryptionKeyIv",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "PasswordIv");

            migrationBuilder.RenameColumn(
                name: "EncryptionKeyAuthTag",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "PasswordAuthTag");

            migrationBuilder.RenameColumn(
                name: "EncryptedSignatureKey",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "EncryptedUsername");

            migrationBuilder.RenameColumn(
                name: "EncryptedEncryptionKey",
                schema: "brokerage_sync",
                table: "IBKRCredentials",
                newName: "EncryptedPassword");
        }
    }
}
