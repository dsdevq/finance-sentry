using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Subscriptions.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "detected_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MerchantNameNormalized = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MerchantNameDisplay = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cadence = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AverageAmount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    LastKnownAmount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    LastChargeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextExpectedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false, defaultValue: "active"),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    DismissedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detected_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_detected_subscription_last_charge",
                table: "detected_subscriptions",
                columns: new[] { "UserId", "LastChargeDate" },
                filter: "\"Status\" = 'active'");

            migrationBuilder.CreateIndex(
                name: "idx_detected_subscription_user_merchant",
                table: "detected_subscriptions",
                columns: new[] { "UserId", "MerchantNameNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_detected_subscription_user_status",
                table: "detected_subscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detected_subscriptions");
        }
    }
}
