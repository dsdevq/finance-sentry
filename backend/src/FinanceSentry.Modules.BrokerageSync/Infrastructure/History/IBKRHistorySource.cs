namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.History;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.Extensions.Logging;

public sealed class IBKRHistorySource(
    IIBKRCredentialRepository credentialRepository,
    IBrokerAdapter adapter,
    IBKRGatewayClient gatewayClient,
    ILogger<IBKRHistorySource> logger) : IProviderMonthlyHistorySource
{
    private readonly IIBKRCredentialRepository _credentialRepository = credentialRepository;
    private readonly IBrokerAdapter _adapter = adapter;
    private readonly IBKRGatewayClient _gatewayClient = gatewayClient;
    private readonly ILogger<IBKRHistorySource> _logger = logger;

    public async Task<IReadOnlyList<ProviderMonthlyBalance>> GetMonthlyBalancesAsync(
        Guid userId, CancellationToken ct = default)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(userId, ct);
        if (credential is null || !credential.IsActive)
            return [];

        try
        {
            await _adapter.EnsureSessionAsync(ct);
            var accountId = await _adapter.GetAccountIdAsync(ct);

            var performance = await _gatewayClient.GetPerformanceAsync(accountId, ct);
            if (performance is null || performance.Nav.Data.Count == 0)
                return [];

            return performance.Nav.Data
                .Where(e => DateOnly.TryParseExact(e.Date, "yyyyMMdd", out _))
                .GroupBy(e => MonthEnd(DateOnly.ParseExact(e.Date, "yyyyMMdd", null)))
                .Select(g =>
                {
                    var lastNav = g.OrderBy(e => e.Date).Last().Nav;
                    return new ProviderMonthlyBalance(g.Key, lastNav, "brokerage");
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IBKR history fetch failed for user {UserId} during backfill — returning empty", userId);
            return [];
        }
    }

    private static DateOnly MonthEnd(DateOnly d)
    {
        var daysInMonth = DateTime.DaysInMonth(d.Year, d.Month);
        return new DateOnly(d.Year, d.Month, daysInMonth);
    }
}
