using FinanceSentry.Modules.Research.Application.Commands;
using FinanceSentry.Modules.Research.Application.Queries;

namespace FinanceSentry.Mcp.Responses;

/// <summary>
/// Enriched result of <c>run_thesis_monitor</c> (feature 035): the run summary PLUS the resulting
/// breaks, so the agent's always-back-to-back monitor→list-breaks dance is one call. The pure-read
/// <c>list_thesis_breaks</c> tool is kept for status checks that must not re-evaluate or raise alerts.
/// </summary>
/// <param name="Summary">The existing run summary — unchanged shape.</param>
/// <param name="Breaks">The caller's breaks after the run — same shape <c>list_thesis_breaks</c> returns.</param>
public sealed record ThesisMonitorResult(
    ThesisMonitorRunSummary Summary,
    IReadOnlyList<ThesisBreakView> Breaks);
