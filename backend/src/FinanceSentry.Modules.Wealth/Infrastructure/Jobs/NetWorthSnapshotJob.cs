namespace FinanceSentry.Modules.Wealth.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using Hangfire;

public class NetWorthSnapshotJob(
    IBankingTotalsReader bankingTotals,
    ICryptoHoldingsReader cryptoReader,
    IBrokerageHoldingsReader brokerageReader,
    INetWorthSnapshotService snapshotService)
{
    private readonly IBankingTotalsReader _bankingTotals = bankingTotals ?? throw new ArgumentNullException(nameof(bankingTotals));
    private readonly ICryptoHoldingsReader _cryptoReader = cryptoReader ?? throw new ArgumentNullException(nameof(cryptoReader));
    private readonly IBrokerageHoldingsReader _brokerageReader = brokerageReader ?? throw new ArgumentNullException(nameof(brokerageReader));
    private readonly INetWorthSnapshotService _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var userIds = await _bankingTotals.GetActiveUserIdsAsync(ct);
        foreach (var userId in userIds)
            await TakeSnapshotAsync(userId, ct);
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteForUserAsync(Guid userId, CancellationToken ct = default)
        => await TakeSnapshotAsync(userId, ct);

    private async Task TakeSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var bankingTotal = await _bankingTotals.GetTotalUsdAsync(userId, ct);

        var cryptoHoldings = await _cryptoReader.GetHoldingsAsync(userId, ct);
        var cryptoTotal = cryptoHoldings.Sum(h => h.UsdValue);

        var brokerageHoldings = await _brokerageReader.GetHoldingsAsync(userId, ct);
        var brokerageTotal = brokerageHoldings.Sum(h => h.UsdValue);

        var now = DateTime.UtcNow;
        var snapshotDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

        await _snapshotService.PersistSnapshotAsync(userId, new NetWorthSnapshotData(
            snapshotDate, bankingTotal, brokerageTotal, cryptoTotal), ct);
    }
}
