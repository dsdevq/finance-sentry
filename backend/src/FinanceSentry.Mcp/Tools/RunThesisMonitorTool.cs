using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.Application.Commands;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class RunThesisMonitorTool(
    ICommandHandler<RunThesisMonitorCommand, ThesisMonitorRunSummary> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "run_thesis_monitor")]
    [Description("Runs the deterministic thesis-break evaluation for the caller's active theses now (same code path as the scheduled job). Persists any break-state changes and raises/resolves alerts as a side effect.")]
    public async Task<ThesisMonitorRunSummary?> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return null;
        }

        return await handler.Handle(new RunThesisMonitorCommand(effective.Value), cancellationToken);
    }
}
