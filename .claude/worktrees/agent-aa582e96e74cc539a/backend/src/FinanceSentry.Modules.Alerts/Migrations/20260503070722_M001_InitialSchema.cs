using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Alerts.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_alert_dedup",
                table: "alerts",
                columns: new[] { "UserId", "Type", "ReferenceId" },
                unique: true,
                filter: "\"IsResolved\" = false AND \"IsDismissed\" = false");

            migrationBuilder.CreateIndex(
                name: "idx_alert_purge",
                table: "alerts",
                column: "CreatedAt",
                filter: "\"IsResolved\" = true OR \"IsDismissed\" = true");

            migrationBuilder.CreateIndex(
                name: "idx_alert_user_created",
                table: "alerts",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true },
                filter: "\"IsDismissed\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");
        }
    }
}
