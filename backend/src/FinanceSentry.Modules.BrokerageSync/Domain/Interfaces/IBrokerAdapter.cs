namespace FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;

public interface IBrokerAdapter
{
    string BrokerName { get; }
    Task AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task<string> GetAccountIdAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BrokerPosition>> GetPositionsAsync(string accountId, CancellationToken ct = default);
}

public sealed record BrokerPosition(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal UsdValue);
