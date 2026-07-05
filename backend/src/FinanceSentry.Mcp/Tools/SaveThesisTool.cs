using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Commands;
using FinanceSentry.Modules.Research.Domain;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class SaveThesisTool(
    ICommandHandler<SaveThesisCommand, ThesisDto> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "save_thesis")]
    [Description("Creates or updates an investment thesis. Pass id=null to create; pass an existing id to update. At least one invalidationTrigger is expected for thesis-break detection to work.")]
    public async Task<ThesisDto?> ExecuteAsync(
        [Description("Ticker symbol the thesis is about.")] string ticker,
        [Description("Free-form thesis narrative — the investment case in Denys's words.")] string thesisText,
        [Description("Structured key data points that back the thesis.")] IReadOnlyList<ThesisDataPoint> keyDataPoints,
        [Description("Upcoming catalysts (dates + events) that could confirm or break the thesis.")] IReadOnlyList<ThesisCatalyst> catalysts,
        [Description("Invalidation triggers — quantitative conditions that mark the thesis broken (e.g. gross_margin<40).")] IReadOnlyList<ThesisInvalidationTrigger> invalidationTriggers,
        [Description("Thesis id when updating; null to create.")] Guid? id = null,
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return null;
        }

        return await handler.Handle(
            new SaveThesisCommand(
                effective.Value,
                id,
                ticker,
                thesisText,
                keyDataPoints ?? [],
                catalysts ?? [],
                invalidationTriggers ?? []),
            cancellationToken);
    }
}
