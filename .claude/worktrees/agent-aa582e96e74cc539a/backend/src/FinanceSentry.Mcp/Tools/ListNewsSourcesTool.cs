using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ListNewsSourcesTool(
    IQueryHandler<ListNewsSourcesQuery, IReadOnlyList<NewsSourceDto>> handler)
{
    [McpServerTool(Name = "list_news_sources")]
    [Description("Lists every registered news source (market-wide defaults + thesis-attached), enabled and disabled, with per-source health: kind, url, keywords, thesisId, consecutiveFailures, lastSuccessAt, lastFailureReason. Use this to see which sources are feeding the news pipeline and whether any are failing before relying on their coverage.")]
    public async Task<IReadOnlyList<NewsSourceDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await handler.Handle(new ListNewsSourcesQuery(), cancellationToken);
    }
}
