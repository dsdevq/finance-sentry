using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M011_CounterpartyFlows : Migration
    {
        // Well-known GUIDs for default counterparties (UserId = Guid.Empty = system defaults).
        private static readonly Guid LyudmilaId = new("11111111-0000-0000-0000-000000000001");
        private static readonly Guid YelyzavetaId = new("11111111-0000-0000-0000-000000000002");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "counterparties",
                schema: "bank_sync",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FlowRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counterparties", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_counterparty_user_id",
                schema: "bank_sync",
                table: "counterparties",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "counterparty_rules",
                schema: "bank_sync",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counterparty_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_counterparty_rules_counterparties_CounterpartyId",
                        column: x => x.CounterpartyId,
                        principalSchema: "bank_sync",
                        principalTable: "counterparties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_counterparty_rule_counterparty_id",
                schema: "bank_sync",
                table: "counterparty_rules",
                column: "CounterpartyId");

            // Seed FAMILY_SUPPORT into the categories reference table.
            migrationBuilder.InsertData(
                schema: "bank_sync",
                table: "categories",
                columns: ["Key", "Label", "SortOrder"],
                values: new object[] { "FAMILY_SUPPORT", "Family Support", 135 });

            // Seed system-default counterparties (UserId = Guid.Empty applies to all users).
            migrationBuilder.InsertData(
                schema: "bank_sync",
                table: "counterparties",
                columns: ["Id", "UserId", "Name", "FlowRole", "CreatedAt"],
                values: new object[,]
                {
                    {
                        LyudmilaId,
                        Guid.Empty,
                        "Людмила Сичова (Мама)",
                        "family_support",
                        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        YelyzavetaId,
                        Guid.Empty,
                        "Єлизавета Морозова (Ліза)",
                        "family_support",
                        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    }
                });

            // Seed match rules for the default counterparties.
            migrationBuilder.InsertData(
                schema: "bank_sync",
                table: "counterparty_rules",
                columns: ["Id", "CounterpartyId", "MatchType", "Pattern", "CreatedAt"],
                values: new object[,]
                {
                    // Людмила Сичова — matches description "Людмила Сичова" or "мама"
                    {
                        new Guid("22222222-0000-0000-0000-000000000001"),
                        LyudmilaId,
                        "description_contains",
                        "Людмила Сичова",
                        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000002"),
                        LyudmilaId,
                        "description_contains",
                        "мама",
                        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    },
                    // Єлизавета Морозова — matches description "Єлизавета Морозова" or "Ліза"
                    {
                        new Guid("22222222-0000-0000-0000-000000000003"),
                        YelyzavetaId,
                        "description_contains",
                        "Єлизавета Морозова",
                        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000004"),
                        YelyzavetaId,
                        "description_contains",
                        "Ліза",
                        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "bank_sync",
                table: "categories",
                keyColumn: "Key",
                keyValue: "FAMILY_SUPPORT");

            migrationBuilder.DropTable(
                name: "counterparty_rules",
                schema: "bank_sync");

            migrationBuilder.DropTable(
                name: "counterparties",
                schema: "bank_sync");
        }
    }
}
