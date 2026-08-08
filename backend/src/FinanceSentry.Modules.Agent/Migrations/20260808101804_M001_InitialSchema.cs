using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceSentry.Modules.Agent.Migrations
{
    /// <inheritdoc />
    public partial class M001_InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agent");

            migrationBuilder.CreateTable(
                name: "agent_conversations",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ModelId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "agent_messages",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ToolCallsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ToolResultsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agent_messages_agent_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "agent",
                        principalTable: "agent_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_agent_conversations_user_updated",
                schema: "agent",
                table: "agent_conversations",
                columns: new[] { "UserId", "UpdatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_agent_messages_conversation_created",
                schema: "agent",
                table: "agent_messages",
                columns: new[] { "ConversationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_messages",
                schema: "agent");

            migrationBuilder.DropTable(
                name: "agent_conversations",
                schema: "agent");
        }
    }
}
