using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.BankSync.Migrations
{
    /// <summary>
    /// Data-only migration: seeds the system-default counterparty for investment routing.
    /// Money leaving the bank for a brokerage or exchange is not spending, so it needs a
    /// flow role of its own — without it the dashboard cannot say how much of what was
    /// kept was actually put to work.
    /// </summary>
    [DbContext(typeof(BankSyncDbContext))]
    [Migration("20260903000000_M012_InvestmentRoutingCounterparty")]
    public partial class M012_InvestmentRoutingCounterparty : Migration
    {
        private static readonly Guid InvestmentRoutingId = new("11111111-0000-0000-0000-000000000003");
        private static readonly DateTime SeededAt = new(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "bank_sync",
                table: "counterparties",
                columns: ["Id", "UserId", "Name", "FlowRole", "CreatedAt"],
                columnTypes: ["uuid", "uuid", "character varying(255)", "character varying(50)", "timestamp with time zone"],
                values: new object[] { InvestmentRoutingId, Guid.Empty, "Investment routing", "investment", SeededAt });

            migrationBuilder.InsertData(
                schema: "bank_sync",
                table: "counterparty_rules",
                columns: ["Id", "CounterpartyId", "MatchType", "Pattern", "CreatedAt"],
                columnTypes: ["uuid", "uuid", "character varying(50)", "character varying(255)", "timestamp with time zone"],
                values: new object[,]
                {
                    // The venues this user actually routes to (Binance + IBKR), matched on the
                    // statement description and on the normalised merchant name.
                    {
                        new Guid("22222222-0000-0000-0000-000000000011"),
                        InvestmentRoutingId,
                        "description_contains",
                        "Binance",
                        SeededAt
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000012"),
                        InvestmentRoutingId,
                        "merchant_name_contains",
                        "Binance",
                        SeededAt
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000013"),
                        InvestmentRoutingId,
                        "description_contains",
                        "Interactive Brokers",
                        SeededAt
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000014"),
                        InvestmentRoutingId,
                        "merchant_name_contains",
                        "Interactive Brokers",
                        SeededAt
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rules first — they carry the FK to the counterparty.
            foreach (var ruleId in new[]
            {
                new Guid("22222222-0000-0000-0000-000000000011"),
                new Guid("22222222-0000-0000-0000-000000000012"),
                new Guid("22222222-0000-0000-0000-000000000013"),
                new Guid("22222222-0000-0000-0000-000000000014")
            })
            {
                migrationBuilder.DeleteData(
                    schema: "bank_sync",
                    table: "counterparty_rules",
                    keyColumn: "Id",
                    keyColumnType: "uuid",
                    keyValue: ruleId);
            }

            migrationBuilder.DeleteData(
                schema: "bank_sync",
                table: "counterparties",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: InvestmentRoutingId);
        }
    }
}
