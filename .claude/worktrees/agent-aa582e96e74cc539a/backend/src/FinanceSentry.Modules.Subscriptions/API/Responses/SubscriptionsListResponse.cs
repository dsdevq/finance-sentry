namespace FinanceSentry.Modules.Subscriptions.API.Responses;

public record SubscriptionsListResponse(
    IReadOnlyList<SubscriptionDto> Items,
    int TotalCount,
    bool HasInsufficientHistory);
