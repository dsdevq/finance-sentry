using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Subscriptions.Migrations
{
    /// <inheritdoc />
    public partial class M005_AddInstallmentStartDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "detected_subscriptions",
                type: "date",
                nullable: true);

            // The mortgage predates the 13-month detection window, so its first *observed*
            // charge (Jun 2026) is not its start and would understate how far UAH/USD has
            // moved. Anchored to a 12-year term ending May 2036. Derived, not authoritative —
            // editable via PATCH /subscriptions/installments/{id}/term.
            migrationBuilder.Sql(
                """
                UPDATE public.detected_subscriptions
                SET "StartDate" = DATE '2024-05-01'
                WHERE "Kind" = 'installment'
                  AND "EndDate" = DATE '2036-05-01'
                  AND "StartDate" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "detected_subscriptions");
        }
    }
}
