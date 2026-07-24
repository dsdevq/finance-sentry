using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetAnalystActionsTool(
    IQueryHandler<GetAnalystActionsQuery, AnalystActionsResult> handler)
{
    private const int DefaultLookbackDays = 30;
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    [McpServerTool(Name = "get_analyst_actions")]
    [Description("Query street/analyst actions (upgrades, downgrades, coverage initiations, price-target changes, reiterations) ingested nightly from free public sources (MarketBeat market-wide sweep + Yahoo per-ticker). Market-wide — ticker is OPTIONAL; omit it to see actions across the whole covered universe, including names Denys does not hold. Every row carries its source for the 'every claim carries a fresh source' rule. The 'coverage' field distinguishes 'notInUniverse' (ticker isn't tracked) from an empty result (tracked, but no recent actions).")]
    public async Task<AnalystActionsResult> ExecuteAsync(
        [Description("Optional ticker filter (e.g. MU). Omit for a whole-universe query.")] string? ticker = null,
        [Description("Optional ISO-8601 cutoff date. Only actions on/after this date are returned. Default: 30 days ago.")] DateTimeOffset? since = null,
        [Description("Optional action-type filter: Upgrade | Downgrade | Initiate | TargetChange | Reiterate | TopIdea.")] string? actionType = null,
        [Description("Optional companion-event reference id. When provided, resolves the exact analyst action row and ignores ticker/since/actionType filters.")] Guid? referenceId = null,
        [Description("Max results, default 50, max 200.")] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        var sinceDate = since is { } s
            ? DateOnly.FromDateTime(s.UtcDateTime)
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-DefaultLookbackDays));

        var effectiveLimit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);

        return await handler.Handle(
            new GetAnalystActionsQuery(ticker, sinceDate, actionType, effectiveLimit, referenceId), cancellationToken);
    }
}
