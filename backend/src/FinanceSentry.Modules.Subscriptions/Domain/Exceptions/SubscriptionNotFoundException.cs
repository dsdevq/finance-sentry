namespace FinanceSentry.Modules.Subscriptions.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class SubscriptionNotFoundException() : ApiException(404, "SUBSCRIPTION_NOT_FOUND", "Subscription not found.");
