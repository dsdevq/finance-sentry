using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Subscriptions.API.Responses;
using FinanceSentry.Modules.Subscriptions.Application.Queries;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ListSubscriptionsTool(
    IQueryHandler<GetSubscriptionsQuery, SubscriptionsListResponse> subscriptionsHandler,
    ILogger<ListSubscriptionsTool> logger) : IReadOnlyMcpTool
{
    private readonly IQueryHandler<GetSubscriptionsQuery, SubscriptionsListResponse> _subscriptionsHandler = subscriptionsHandler;
    private readonly ILogger<ListSubscriptionsTool> _logger = logger;

    public string ToolName => "list_subscriptions";

    [McpServerTool(Name = "list_subscriptions")]
    [Description("Returns detected recurring charges (subscriptions) for a user, excluding dismissed ones.")]
    public async Task<IReadOnlyList<SubscriptionEntry>> ExecuteAsync(
        [Description("The user's unique identifier.")] Guid userId,
        CancellationToken cancellationToken = default)
    {
        SubscriptionsListResponse response;
        try
        {
            response = await _subscriptionsHandler.Handle(
                new GetSubscriptionsQuery(userId.ToString(), IncludeDismissed: false),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Subscriptions query unavailable for user {UserId}; returning empty list.", userId);
            return [];
        }

        return response.Items
            .Select(s => new SubscriptionEntry(
                s.Id.ToString(),
                s.MerchantName,
                s.MonthlyEquivalent,
                s.Currency,
                s.LastChargeDate,
                s.DetectedAt))
            .ToList();
    }
}

public sealed record SubscriptionEntry(
    string SubscriptionId,
    string Merchant,
    decimal EstimatedMonthlyAmount,
    string Currency,
    DateOnly LastChargedAt,
    DateTimeOffset DetectedAt);
