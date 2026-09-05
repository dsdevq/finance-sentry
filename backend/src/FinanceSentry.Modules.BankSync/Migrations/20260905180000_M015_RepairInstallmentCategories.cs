using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <summary>
    /// Data-only repair (#581): installment repayments («Погашення …», monomarket, Pandora)
    /// and the platinum-card service fee carry the wire-transfer MCC 4829 / misleading
    /// wordings, and the Monobank ingest path never consulted the merchant-keyword bridge —
    /// so once mcc 4829 was remapped to TRANSFER_OUT, real spending started vanishing from
    /// outflow. The adapter now checks keywords first; this migration re-categorizes the
    /// rows ingested while the gap was open, using the same wordings the keywords match.
    /// </summary>
    [DbContext(typeof(BankSyncDbContext))]
    [Migration("20260905180000_M015_RepairInstallmentCategories")]
    public partial class M015_RepairInstallmentCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE bank_sync."Transactions"
                SET "MerchantCategory" = 'LOAN_PAYMENTS', "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "MerchantCategory" = 'TRANSFER_OUT'
                  AND ("Description" ILIKE '%погашення%'
                       OR "Description" ILIKE '%monomarket%'
                       OR "Description" ILIKE '%щомісячний платіж%'
                       OR "Description" ILIKE 'Платіж Pandora%');
                """);

            migrationBuilder.Sql(
                """
                UPDATE bank_sync."Transactions"
                SET "MerchantCategory" = 'BANK_FEES', "UpdatedAt" = CURRENT_TIMESTAMP
                WHERE "MerchantCategory" = 'TRANSFER_OUT'
                  AND "Description" ILIKE '%платинової картки%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair — the pre-repair miscategorization is not worth restoring.
        }
    }
}
