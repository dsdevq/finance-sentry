using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ListThesesTool(
    IQueryHandler<GetThesesQuery, IReadOnlyList<ThesisDto>> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "list_theses")]
    [Description("Lists the caller's investment theses, including key data points, catalysts, and invalidation triggers.")]
    public async Task<IReadOnlyList<ThesisDto>> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return [];
        }

        return await handler.Handle(new GetThesesQuery(effective.Value), cancellationToken);
    }
}
