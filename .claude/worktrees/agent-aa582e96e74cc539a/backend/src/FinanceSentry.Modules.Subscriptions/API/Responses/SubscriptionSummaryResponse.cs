namespace FinanceSentry.Modules.Subscriptions.API.Responses;

public record SubscriptionSummaryResponse(
    decimal TotalMonthlyEstimate,
    decimal TotalAnnualEstimate,
    int ActiveCount,
    int PotentiallyCancelledCount,
    string Currency);
