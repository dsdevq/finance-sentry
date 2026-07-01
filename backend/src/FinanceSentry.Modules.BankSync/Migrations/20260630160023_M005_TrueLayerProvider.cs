using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M005_TrueLayerProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TrueLayerConnectionId",
                schema: "bank_sync",
                table: "BankAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrueLayerConnections",
                schema: "bank_sync",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "CREATED"),
                    EncryptedRefreshToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    Iv = table.Column<byte[]>(type: "bytea", nullable: false),
                    AuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ConnectionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrueLayerConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TrueLayerConnectionId",
                schema: "bank_sync",
                table: "BankAccounts",
                column: "TrueLayerConnectionId");

            migrationBuilder.CreateIndex(
                name: "idx_truelayer_connection_reference_unique",
                schema: "bank_sync",
                table: "TrueLayerConnections",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_truelayer_connection_user_id",
                schema: "bank_sync",
                table: "TrueLayerConnections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_truelayer_connection_user_provider_unique",
                schema: "bank_sync",
                table: "TrueLayerConnections",
                columns: new[] { "UserId", "ProviderId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_TrueLayerConnections_TrueLayerConnectionId",
                schema: "bank_sync",
                table: "BankAccounts",
                column: "TrueLayerConnectionId",
                principalSchema: "bank_sync",
                principalTable: "TrueLayerConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_TrueLayerConnections_TrueLayerConnectionId",
                schema: "bank_sync",
                table: "BankAccounts");

            migrationBuilder.DropTable(
                name: "TrueLayerConnections",
                schema: "bank_sync");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_TrueLayerConnectionId",
                schema: "bank_sync",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "TrueLayerConnectionId",
                schema: "bank_sync",
                table: "BankAccounts");
        }
    }
}
