using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.Application.Commands;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ScoreCandidateTool(
    ICommandHandler<ScoreCandidateCommand, ScoreCandidateResult> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "score_candidate")]
    [Description("Scores a conviction candidate deterministically (018 structure + EDGAR fundamentals + crowding + IPS fit). Creates a User-source candidate the first time, or appends a re-score to the existing active one. Sub-scores are 0-100 or null when not evaluable (never faked); there is no composite score. Every sub-score cites its raw inputs in the evidence.")]
    public async Task<ScoreCandidateResult?> ExecuteAsync(
        [Description("Ticker symbol to score, e.g. MSFT.")] string ticker,
        [Description("Optional contemporaneous reasoning captured at nomination time (decision journal).")] string? decisionNote = null,
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return null;
        }

        return await handler.Handle(
            new ScoreCandidateCommand(effective.Value, ticker, decisionNote), cancellationToken);
    }
}
