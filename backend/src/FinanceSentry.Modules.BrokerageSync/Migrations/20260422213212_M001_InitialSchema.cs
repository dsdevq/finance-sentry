using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BrokerageSync.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerageHoldings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InstrumentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(30,10)", precision: 30, scale: 10, nullable: false),
                    UsdValue = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ibkr")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerageHoldings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IBKRCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedUsername = table.Column<byte[]>(type: "bytea", nullable: false),
                    UsernameIv = table.Column<byte[]>(type: "bytea", nullable: false),
                    UsernameAuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptedPassword = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordIv = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordAuthTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IBKRCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerageHoldings_UserId_Symbol_Provider",
                table: "BrokerageHoldings",
                columns: new[] { "UserId", "Symbol", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IBKRCredentials_UserId",
                table: "IBKRCredentials",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerageHoldings");

            migrationBuilder.DropTable(
                name: "IBKRCredentials");
        }
    }
}
