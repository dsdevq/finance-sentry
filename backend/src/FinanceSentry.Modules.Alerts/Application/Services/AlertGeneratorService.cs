namespace FinanceSentry.Modules.Alerts.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Alerts.Domain;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public class AlertGeneratorService(IAlertRepository alerts) : IAlertGeneratorService
{
    private readonly IAlertRepository _alerts = alerts;

    public async Task GenerateLowBalanceAlertAsync(
        Guid userId, Guid accountId, string accountName,
        decimal balance, decimal threshold, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.LowBalance, accountId, ct);
        if (existing is not null) return;

        await _alerts.AddAsync(new Alert
        {
            UserId = userId,
            Type = AlertType.LowBalance,
            Severity = AlertSeverity.Warning,
            Title = $"Low balance on {accountName}",
            Message = $"Your {accountName} balance ({balance:C}) has dropped below your {threshold:C} threshold.",
            ReferenceId = accountId,
            ReferenceLabel = accountName,
        }, ct);
    }

    public async Task ResolveLowBalanceAlertAsync(
        Guid userId, Guid accountId, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.LowBalance, accountId, ct);
        if (existing is null) return;
        await _alerts.ResolveAsync(existing.Id, ct);
    }

    public async Task GenerateSyncFailureAlertAsync(
        Guid userId, string provider, Guid? accountId, string? accountName,
        string? errorCode, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.SyncFailure, accountId, ct);
        if (existing is not null) return;

        var label = accountName ?? provider;
        var detail = errorCode is null ? string.Empty : $" (error: {errorCode})";

        await _alerts.AddAsync(new Alert
        {
            UserId = userId,
            Type = AlertType.SyncFailure,
            Severity = AlertSeverity.Error,
            Title = $"Sync failed for {label}",
            Message = $"We couldn't sync your {provider} account{detail}. Please reconnect or check your credentials.",
            ReferenceId = accountId,
            ReferenceLabel = accountName,
        }, ct);
    }

    public async Task ResolveSyncFailureAlertAsync(
        Guid userId, string provider, Guid? accountId, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.SyncFailure, accountId, ct);
        if (existing is null) return;
        await _alerts.ResolveAsync(existing.Id, ct);
    }

    public Task DeleteAlertsForAccountAsync(Guid accountId, CancellationToken ct = default)
        => _alerts.DeleteByReferenceIdAsync(accountId, ct);

    public async Task GenerateUnusualSpendAlertAsync(
        Guid userId, string category, decimal currentMonthSpend,
        decimal averageMonthlySpend, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.UnusualSpend, null, ct);
        if (existing is not null && existing.ReferenceLabel == category) return;

        await _alerts.AddAsync(new Alert
        {
            UserId = userId,
            Type = AlertType.UnusualSpend,
            Severity = AlertSeverity.Info,
            Title = $"Unusual spend in {category}",
            Message = $"Your {category} spend this month ({currentMonthSpend:C}) is more than 2× your 3-month average ({averageMonthlySpend:C}).",
            ReferenceId = null,
            ReferenceLabel = category,
        }, ct);
    }
}
