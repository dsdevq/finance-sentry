using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Radar.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "radar");

            migrationBuilder.CreateTable(
                name: "daily_bars",
                schema: "radar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    AdjClose = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_bars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "radar_signals",
                schema: "radar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Scanner = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DedupKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radar_signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "radar_universe_members",
                schema: "radar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radar_universe_members", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_daily_bars_ticker_date",
                schema: "radar",
                table: "daily_bars",
                columns: new[] { "Ticker", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_radar_signals_dedup",
                schema: "radar",
                table: "radar_signals",
                column: "DedupKey");

            migrationBuilder.CreateIndex(
                name: "idx_radar_signals_scanner_type",
                schema: "radar",
                table: "radar_signals",
                columns: new[] { "Scanner", "SignalType" });

            migrationBuilder.CreateIndex(
                name: "idx_radar_signals_subject",
                schema: "radar",
                table: "radar_signals",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "idx_radar_signals_timestamp",
                schema: "radar",
                table: "radar_signals",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_radar_universe_ticker",
                schema: "radar",
                table: "radar_universe_members",
                column: "Ticker",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_bars",
                schema: "radar");

            migrationBuilder.DropTable(
                name: "radar_signals",
                schema: "radar");

            migrationBuilder.DropTable(
                name: "radar_universe_members",
                schema: "radar");
        }
    }
}
