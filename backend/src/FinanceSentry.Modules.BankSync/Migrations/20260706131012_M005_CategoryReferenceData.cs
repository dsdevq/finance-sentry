using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M005_CategoryReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mcc",
                schema: "bank_sync",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCategory",
                schema: "bank_sync",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "bank_sync",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "mcc_categories",
                schema: "bank_sync",
                columns: table => new
                {
                    Mcc = table.Column<int>(type: "integer", nullable: false),
                    CategoryKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcc_categories", x => x.Mcc);
                    table.ForeignKey(
                        name: "FK_mcc_categories_categories_CategoryKey",
                        column: x => x.CategoryKey,
                        principalSchema: "bank_sync",
                        principalTable: "categories",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_mcc_category_category_key",
                schema: "bank_sync",
                table: "mcc_categories",
                column: "CategoryKey");

            // Backfill: translate legacy lowercase category keys to the adopted Plaid PFC
            // primary keys. Existing rows carry no raw MCC/PFC signal, so unresolved legacy
            // values ("other") become UNCATEGORIZED; future syncs re-resolve from raw signal.
            migrationBuilder.Sql(
                """
                UPDATE bank_sync."Transactions" SET "MerchantCategory" = CASE "MerchantCategory"
                    WHEN 'food_and_drink' THEN 'FOOD_AND_DRINK'
                    WHEN 'transport'      THEN 'TRANSPORTATION'
                    WHEN 'shopping'       THEN 'GENERAL_MERCHANDISE'
                    WHEN 'entertainment'  THEN 'ENTERTAINMENT'
                    WHEN 'health'         THEN 'MEDICAL'
                    WHEN 'utilities'      THEN 'RENT_AND_UTILITIES'
                    WHEN 'travel'         THEN 'TRAVEL'
                    WHEN 'housing'        THEN 'HOME_IMPROVEMENT'
                    ELSE 'UNCATEGORIZED'
                END
                WHERE "MerchantCategory" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mcc_categories",
                schema: "bank_sync");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "bank_sync");

            migrationBuilder.DropColumn(
                name: "Mcc",
                schema: "bank_sync",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SourceCategory",
                schema: "bank_sync",
                table: "Transactions");
        }
    }
}
