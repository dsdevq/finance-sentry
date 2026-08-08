using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Radar.Migrations
{
    /// <inheritdoc />
    public partial class M002_RegimeReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "regime_readings",
                schema: "radar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VolatilityAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    VolatilityRegime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VixLevel = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    VixSma = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    VixTrend = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RatesAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    RatesRegime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Dgs10 = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    Dgs2 = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    Spread = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    RecessionWarning = table.Column<bool>(type: "boolean", nullable: false),
                    GrowthValueTilt = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regime_readings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_regime_readings_computed_at",
                schema: "radar",
                table: "regime_readings",
                column: "ComputedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "regime_readings",
                schema: "radar");
        }
    }
}
