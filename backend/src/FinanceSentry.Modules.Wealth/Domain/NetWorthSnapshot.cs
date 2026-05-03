namespace FinanceSentry.Modules.Wealth.Domain;

public sealed class NetWorthSnapshot
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly SnapshotDate { get; init; }
    public decimal BankingTotal { get; init; }
    public decimal BrokerageTotal { get; init; }
    public decimal CryptoTotal { get; init; }
    public decimal TotalNetWorth { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTimeOffset TakenAt { get; init; }
}
