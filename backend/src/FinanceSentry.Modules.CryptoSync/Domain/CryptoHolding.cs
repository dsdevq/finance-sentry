namespace FinanceSentry.Modules.CryptoSync.Domain;

public sealed class CryptoHolding
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Asset { get; private set; } = string.Empty;
    public decimal FreeQuantity { get; private set; }
    public decimal LockedQuantity { get; private set; }
    public decimal UsdValue { get; private set; }
    public DateTime SyncedAt { get; private set; }
    public string Provider { get; private set; } = "binance";

    private CryptoHolding() { }

    public static CryptoHolding Create(
        Guid userId,
        string asset,
        decimal freeQuantity,
        decimal lockedQuantity,
        decimal usdValue,
        string provider = "binance")
    {
        return new CryptoHolding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Asset = asset,
            FreeQuantity = freeQuantity,
            LockedQuantity = lockedQuantity,
            UsdValue = usdValue,
            SyncedAt = DateTime.UtcNow,
            Provider = provider,
        };
    }

    public void Update(decimal freeQuantity, decimal lockedQuantity, decimal usdValue)
    {
        FreeQuantity = freeQuantity;
        LockedQuantity = lockedQuantity;
        UsdValue = usdValue;
        SyncedAt = DateTime.UtcNow;
    }
}
