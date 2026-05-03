namespace FinanceSentry.Modules.Budgets.Domain;

public class Budget
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public decimal MonthlyLimit { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Budget() { }

    public static Budget Create(Guid userId, string category, decimal monthlyLimit, string currency)
    {
        return new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category,
            MonthlyLimit = monthlyLimit,
            Currency = currency,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateLimit(decimal newLimit)
    {
        MonthlyLimit = newLimit;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
