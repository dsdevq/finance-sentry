using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Alerts.API.Responses;
using FinanceSentry.Modules.Alerts.Application.Queries;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ListActiveAlertsTool(
    IQueryHandler<GetAlertsQuery, AlertsPageResponse> alertsHandler,
    ILogger<ListActiveAlertsTool> logger) : IReadOnlyMcpTool
{
    private readonly IQueryHandler<GetAlertsQuery, AlertsPageResponse> _alertsHandler = alertsHandler;
    private readonly ILogger<ListActiveAlertsTool> _logger = logger;

    public string ToolName => "list_active_alerts";

    [McpServerTool(Name = "list_active_alerts")]
    [Description("Returns unread, unresolved alerts (Fired) for a user. Acknowledged, resolved, and dismissed alerts are excluded.")]
    public async Task<IReadOnlyList<ActiveAlertEntry>> ExecuteAsync(
        [Description("The user's unique identifier.")] Guid userId,
        CancellationToken cancellationToken = default)
    {
        AlertsPageResponse response;
        try
        {
            // "unread" filter restricts to !IsDismissed && !IsRead at the repository layer.
            // pageSize is capped at 100 by the query handler; page 1 is sufficient for an active alert feed.
            response = await _alertsHandler.Handle(
                new GetAlertsQuery(userId, "unread", 1, 100),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alerts query unavailable for user {UserId}; returning empty list.", userId);
            return [];
        }

        return response.Items
            .Where(a => !a.IsResolved)
            .Select(a => new ActiveAlertEntry(
                a.Id.ToString(),
                a.Type,
                a.Severity,
                a.Title,
                a.Message,
                a.CreatedAt,
                "Fired"))
            .ToList();
    }
}

public sealed record ActiveAlertEntry(
    string AlertId,
    string Type,
    string Severity,
    string Title,
    string Message,
    DateTimeOffset FiredAt,
    string Status);
