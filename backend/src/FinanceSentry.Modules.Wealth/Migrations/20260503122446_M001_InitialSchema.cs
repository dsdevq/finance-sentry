using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Wealth.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "net_worth_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BankingTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BrokerageTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CryptoTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNetWorth = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_net_worth_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_net_worth_snapshot_user_date_unique",
                table: "net_worth_snapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                unique: true,
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "net_worth_snapshots");
        }
    }
}
