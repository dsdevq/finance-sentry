using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <inheritdoc />
    public partial class M003_TransactionCategoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE INDEX idx_transaction_user_category_date
                ON public."Transactions" ("UserId", "MerchantCategory", "PostedDate" DESC)
                WHERE "IsActive" = true;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS public.idx_transaction_user_category_date;""");
        }
    }
}
