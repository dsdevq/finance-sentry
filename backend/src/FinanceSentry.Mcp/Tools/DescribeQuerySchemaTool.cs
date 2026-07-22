using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Analytics.API.Responses;
using FinanceSentry.Modules.Analytics.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class DescribeQuerySchemaTool(
    IQueryHandler<DescribeQuerySchema, QuerySchemaDto> queryHandler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "describe_query_schema")]
    [Description(
        "Lists the curated views run_analytics_query can read — each with its columns/types and a "
        + "one-line purpose. Call this first so you write correct SQL. Only the curated per-user views "
        + "are exposed; raw internal tables are never queryable.")]
    public async Task<QuerySchemaDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Identity is required (FR-004) even though the schema card itself is not user-specific.
        if (identity.GetUserId() is null)
        {
            return null;
        }

        return await queryHandler.Handle(new DescribeQuerySchema(), cancellationToken);
    }
}
