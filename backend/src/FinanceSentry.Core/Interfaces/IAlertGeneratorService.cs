namespace FinanceSentry.Core.Interfaces;

public interface IAlertGeneratorService
{
    Task GenerateLowBalanceAlertAsync(
        Guid userId,
        Guid accountId,
        string accountName,
        decimal balance,
        decimal threshold,
        CancellationToken ct = default);

    Task ResolveLowBalanceAlertAsync(
        Guid userId,
        Guid accountId,
        CancellationToken ct = default);

    Task GenerateSyncFailureAlertAsync(
        Guid userId,
        string provider,
        Guid? accountId,
        string? accountName,
        string? errorCode,
        CancellationToken ct = default);

    Task ResolveSyncFailureAlertAsync(
        Guid userId,
        string provider,
        Guid? accountId,
        CancellationToken ct = default);

    Task GenerateUnusualSpendAlertAsync(
        Guid userId,
        string category,
        decimal currentMonthSpend,
        decimal averageMonthlySpend,
        CancellationToken ct = default);

    Task DeleteAlertsForAccountAsync(
        Guid accountId,
        CancellationToken ct = default);
}
