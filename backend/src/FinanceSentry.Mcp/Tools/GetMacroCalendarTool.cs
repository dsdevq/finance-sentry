using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetMacroCalendarTool(
    IQueryHandler<GetMacroCalendarQuery, IReadOnlyList<MacroEventDto>> handler)
{
    [McpServerTool(Name = "get_macro_calendar")]
    [Description("Returns scheduled macro events (FOMC, CPI, NFP, ECB) in a date range. Filter by region (US, EU) and minimum importance (low/medium/high). Use to flag events in the next 24-48h during ledger-scan.")]
    public async Task<IReadOnlyList<MacroEventDto>> ExecuteAsync(
        [Description("Start date (inclusive). Defaults to today if omitted.")] DateOnly? from = null,
        [Description("End date (inclusive). Defaults to today+30 if omitted.")] DateOnly? to = null,
        [Description("Optional region filter (US, EU).")] IReadOnlyList<string>? regions = null,
        [Description("Minimum importance level (low, medium, high). Defaults to no filter.")] string? minImportance = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromEff = from ?? today;
        var toEff = to ?? fromEff.AddDays(30);
        return await handler.Handle(new GetMacroCalendarQuery(fromEff, toEff, regions, minImportance), cancellationToken);
    }
}
