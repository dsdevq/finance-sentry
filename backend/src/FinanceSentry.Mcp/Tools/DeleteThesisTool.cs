using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.Application.Commands;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class DeleteThesisTool(
    ICommandHandler<DeleteThesisCommand, bool> handler,
    IIdentityResolver identity) : IReadOnlyMcpTool
{
    public string ToolName => "delete_thesis";

    public bool IsReadOnly => false;

    [McpServerTool(Name = "delete_thesis")]
    [Description("Deletes an investment thesis by id. Returns true when a row was deleted.")]
    public async Task<bool> ExecuteAsync(
        [Description("Thesis id.")] Guid id,
        [Description("Optional user GUID. Defaults to MCP_TOKEN identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return false;
        }

        return await handler.Handle(new DeleteThesisCommand(effective.Value, id), cancellationToken);
    }
}
