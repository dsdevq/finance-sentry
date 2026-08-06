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

    public decimal? CostBasisUsd { get; private set; }
    public decimal? AverageBuyPriceUsd { get; private set; }
    public decimal? RealizedPnlUsd { get; private set; }
    public DateTime? LastTradeAt { get; private set; }
    public long LastTradeId { get; private set; }
    public int TradeCount { get; private set; }

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

    public void SetCostBasis(
        decimal? costBasisUsd,
        decimal? averageBuyPriceUsd,
        decimal? realizedPnlUsd,
        DateTime? lastTradeAt,
        long lastTradeId,
        int tradeCount)
    {
        CostBasisUsd = costBasisUsd;
        AverageBuyPriceUsd = averageBuyPriceUsd;
        RealizedPnlUsd = realizedPnlUsd;
        LastTradeAt = lastTradeAt;
        LastTradeId = lastTradeId;
        TradeCount = tradeCount;
    }
}
