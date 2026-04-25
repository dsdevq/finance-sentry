using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.CryptoSync.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BinanceCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedApiKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    ApiKeyIv = table.Column<byte[]>(type: "bytea", nullable: false),
                    ApiKeyAuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptedApiSecret = table.Column<byte[]>(type: "bytea", nullable: false),
                    ApiSecretIv = table.Column<byte[]>(type: "bytea", nullable: false),
                    ApiSecretAuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinanceCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoHoldings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Asset = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FreeQuantity = table.Column<decimal>(type: "numeric(30,10)", precision: 30, scale: 10, nullable: false),
                    LockedQuantity = table.Column<decimal>(type: "numeric(30,10)", precision: 30, scale: 10, nullable: false),
                    UsdValue = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "binance")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoHoldings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BinanceCredentials_UserId",
                table: "BinanceCredentials",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoHoldings_UserId_Asset",
                table: "CryptoHoldings",
                columns: new[] { "UserId", "Asset" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BinanceCredentials");

            migrationBuilder.DropTable(
                name: "CryptoHoldings");
        }
    }
}
