using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Companion.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "companion");

            migrationBuilder.CreateTable(
                name: "companion_capture_state",
                schema: "companion",
                columns: table => new
                {
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Watermark = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companion_capture_state", x => x.Source);
                });

            migrationBuilder.CreateTable(
                name: "companion_events",
                schema: "companion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DedupKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceModule = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Disposition = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companion_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "companion_notification_settings",
                schema: "companion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    QuietHoursStartLocal = table.Column<int>(type: "integer", nullable: true),
                    QuietHoursEndLocal = table.Column<int>(type: "integer", nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MaxProactivePerHour = table.Column<int>(type: "integer", nullable: false),
                    DigestHourLocal = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companion_notification_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companion_events_DedupKey",
                schema: "companion",
                table: "companion_events",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companion_events_UserId_CapturedAt",
                schema: "companion",
                table: "companion_events",
                columns: new[] { "UserId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_companion_events_UserId_Disposition_OccurredAt",
                schema: "companion",
                table: "companion_events",
                columns: new[] { "UserId", "Disposition", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_companion_notification_settings_UserId",
                schema: "companion",
                table: "companion_notification_settings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companion_capture_state",
                schema: "companion");

            migrationBuilder.DropTable(
                name: "companion_events",
                schema: "companion");

            migrationBuilder.DropTable(
                name: "companion_notification_settings",
                schema: "companion");
        }
    }
}
