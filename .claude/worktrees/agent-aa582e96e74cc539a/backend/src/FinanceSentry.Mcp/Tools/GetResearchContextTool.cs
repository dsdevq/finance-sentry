using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetResearchContextTool(
    IQueryHandler<GetResearchContextQuery, ResearchContextPacketDto> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "get_research_context")]
    [Description("Builds a bounded, cited research context packet for a thesis or ticker: thesis summary plus "
        + "evidence grouped into thesis, decision_notes, recent_news, filings, postmortems, and other_research. "
        + "Use it before synthesizing research-heavy answers such as \"what changed?\" or \"what breaks this "
        + "thesis?\". This is non-authoritative research context only — it is never the source for current "
        + "portfolio, account, or risk numbers; use the structured tools for those. Scoped to the "
        + "authenticated MCP identity. A null thesis in the response means no thesis context exists.")]
    public async Task<ResearchContextPacketDto?> ExecuteAsync(
        [Description("Thesis GUID. Required when ticker is omitted.")] Guid? thesisId = null,
        [Description("Ticker symbol. Required when thesisId is omitted.")] string? ticker = null,
        [Description("Optional focusing question steering which evidence is selected.")] string? question = null,
        [Description("Optional ISO timestamp freshness lower bound for supporting chunks.")] DateTimeOffset? from = null,
        [Description("Maximum chunks in the packet. Default 12, max 30.")] int? maxChunks = null,
        [Description("Optional source type allow-list: NewsArticle, InvestmentThesis, DecisionNote, ThesisEvent, Postmortem, FilingExcerpt.")]
        string[]? includeSourceTypes = null,
        CancellationToken cancellationToken = default)
    {
        var userId = identity.GetUserId();
        if (userId is null || (thesisId is null && string.IsNullOrWhiteSpace(ticker)))
        {
            return null;
        }

        return await handler.Handle(
            new GetResearchContextQuery(
                userId.Value,
                thesisId,
                ticker,
                question,
                from,
                maxChunks,
                SearchResearchCorpusTool.ParseSourceTypes(includeSourceTypes)),
            cancellationToken);
    }
}
