namespace FinanceSentry.Modules.BrokerageSync.Domain;

public sealed class BrokerageHolding
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string InstrumentType { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UsdValue { get; private set; }
    public DateTime SyncedAt { get; private set; }
    public string Provider { get; private set; } = string.Empty;

    private BrokerageHolding() { }

    public BrokerageHolding(
        Guid userId,
        string symbol,
        string instrumentType,
        decimal quantity,
        decimal usdValue,
        string provider)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Symbol = symbol;
        InstrumentType = instrumentType;
        Quantity = quantity;
        UsdValue = usdValue;
        SyncedAt = DateTime.UtcNow;
        Provider = provider;
    }

    public void Update(decimal quantity, decimal usdValue)
    {
        Quantity = quantity;
        UsdValue = usdValue;
        SyncedAt = DateTime.UtcNow;
    }
}
