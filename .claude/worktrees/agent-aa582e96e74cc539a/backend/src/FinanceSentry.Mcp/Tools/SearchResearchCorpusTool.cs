using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using FinanceSentry.Modules.Research.Domain;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class SearchResearchCorpusTool(
    IQueryHandler<SearchResearchCorpusQuery, ResearchSearchResultDto> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "search_research_corpus")]
    [Description("Searches Finance Sentry's stored research corpus (news, theses, decision notes) with hybrid "
        + "semantic + lexical ranking and returns cited chunks with provenance and scores. This is "
        + "non-authoritative research context only — current balances, holdings, exposure, tax lots, and risk "
        + "verdicts must come from the dedicated structured tools. Scoped to the authenticated MCP identity: "
        + "private documents of other users are never returned.")]
    public async Task<ResearchSearchResultDto> ExecuteAsync(
        [Description("Natural-language search text.")] string query,
        [Description("Optional ticker symbols to filter to.")] string[]? tickers = null,
        [Description("Optional thesis GUID to filter to.")] Guid? thesisId = null,
        [Description("Optional source type filter: NewsArticle, InvestmentThesis, DecisionNote, ThesisEvent, Postmortem, FilingExcerpt.")]
        string[]? sourceTypes = null,
        [Description("Optional ISO timestamp lower bound on publication/capture time.")] DateTimeOffset? from = null,
        [Description("Optional ISO timestamp upper bound on publication/capture time.")] DateTimeOffset? to = null,
        [Description("Maximum results to return. Default 10, max 50.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var userId = identity.GetUserId();
        if (userId is null || string.IsNullOrWhiteSpace(query))
        {
            return new ResearchSearchResultDto(query, [], DateTimeOffset.UtcNow);
        }

        return await handler.Handle(
            new SearchResearchCorpusQuery(
                userId.Value,
                query,
                tickers ?? [],
                thesisId,
                ParseSourceTypes(sourceTypes),
                from,
                to,
                limit),
            cancellationToken);
    }

    public static IReadOnlyList<ResearchDocumentSourceType> ParseSourceTypes(string[]? sourceTypes)
        => (sourceTypes ?? [])
            .Select(s => Enum.TryParse<ResearchDocumentSourceType>(s, ignoreCase: true, out var parsed)
                ? parsed
                : (ResearchDocumentSourceType?)null)
            .Where(s => s is not null)
            .Select(s => s!.Value)
            .ToList();
}
