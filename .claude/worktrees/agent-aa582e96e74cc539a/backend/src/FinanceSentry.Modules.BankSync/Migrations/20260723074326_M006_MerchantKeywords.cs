using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M006_MerchantKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "merchant_keywords",
                schema: "bank_sync",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Keyword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoryKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_keywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_merchant_keywords_categories_CategoryKey",
                        column: x => x.CategoryKey,
                        principalSchema: "bank_sync",
                        principalTable: "categories",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_merchant_keyword_category_key",
                schema: "bank_sync",
                table: "merchant_keywords",
                column: "CategoryKey");

            migrationBuilder.CreateIndex(
                name: "idx_merchant_keyword_keyword_unique",
                schema: "bank_sync",
                table: "merchant_keywords",
                column: "Keyword",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merchant_keywords",
                schema: "bank_sync");
        }
    }
}
