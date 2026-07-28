namespace FinanceSentry.Modules.Subscriptions.Domain;

public static class SubscriptionStatus
{
    public const string Active = "active";
    public const string Dismissed = "dismissed";
    public const string PotentiallyCancelled = "potentially_cancelled";

    /// <summary>Installment fully paid off (term reached, payoff seen, or user-marked).</summary>
    public const string Completed = "completed";
}
