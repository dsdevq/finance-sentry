using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Research.Migrations
{
    /// <inheritdoc />
    public partial class M007_QuoteSessionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStale",
                schema: "research",
                table: "quote_cache",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MarketState",
                schema: "research",
                table: "quote_cache",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegularMarketTime",
                schema: "research",
                table: "quote_cache",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedTicker",
                schema: "research",
                table: "quote_cache",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Session",
                schema: "research",
                table: "quote_cache",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourcePriceTime",
                schema: "research",
                table: "quote_cache",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStale",
                schema: "research",
                table: "quote_cache");

            migrationBuilder.DropColumn(
                name: "MarketState",
                schema: "research",
                table: "quote_cache");

            migrationBuilder.DropColumn(
                name: "RegularMarketTime",
                schema: "research",
                table: "quote_cache");

            migrationBuilder.DropColumn(
                name: "ResolvedTicker",
                schema: "research",
                table: "quote_cache");

            migrationBuilder.DropColumn(
                name: "Session",
                schema: "research",
                table: "quote_cache");

            migrationBuilder.DropColumn(
                name: "SourcePriceTime",
                schema: "research",
                table: "quote_cache");
        }
    }
}
