namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class ResearchTools
{
    [McpServerTool(Name = "get_watchlist", ReadOnly = true, Destructive = false)]
    [Description("Get investment watchlist. Stubbed until Finance Sentry exposes watchlists.")]
    public static JsonElement GetWatchlist()
    {
        return FinanceSentryApiClient.NotYetAvailable("No watchlist endpoint exists yet.");
    }

    [McpServerTool(Name = "get_investment_thesis", ReadOnly = true, Destructive = false)]
    [Description("Get investment thesis records. Stubbed until Finance Sentry exposes thesis data.")]
    public static JsonElement GetInvestmentThesis()
    {
        return FinanceSentryApiClient.NotYetAvailable("No investment thesis endpoint exists yet.");
    }

    [McpServerTool(Name = "get_report_calendar", ReadOnly = true, Destructive = false)]
    [Description("Get report calendar. Stubbed until Finance Sentry exposes report calendar data.")]
    public static JsonElement GetReportCalendar()
    {
        return FinanceSentryApiClient.NotYetAvailable("No report calendar endpoint exists yet.");
    }
}
