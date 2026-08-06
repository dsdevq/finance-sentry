using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Budgets.Migrations
{
    /// <inheritdoc />
    public partial class M003_RemapBudgetCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap legacy lowercase budget categories to the adopted Plaid PFC primary keys,
            // matching the transaction category backfill in BankSync M005.
            migrationBuilder.Sql(
                """
                UPDATE budgets.budgets SET "Category" = CASE "Category"
                    WHEN 'food_and_drink' THEN 'FOOD_AND_DRINK'
                    WHEN 'transport'      THEN 'TRANSPORTATION'
                    WHEN 'shopping'       THEN 'GENERAL_MERCHANDISE'
                    WHEN 'entertainment'  THEN 'ENTERTAINMENT'
                    WHEN 'health'         THEN 'MEDICAL'
                    WHEN 'utilities'      THEN 'RENT_AND_UTILITIES'
                    WHEN 'travel'         THEN 'TRAVEL'
                    WHEN 'housing'        THEN 'HOME_IMPROVEMENT'
                    WHEN 'other'          THEN 'UNCATEGORIZED'
                    ELSE "Category"
                END
                WHERE "Category" IN ('food_and_drink','transport','shopping','entertainment',
                                     'health','utilities','travel','housing','other');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
