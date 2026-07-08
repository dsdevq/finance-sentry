namespace FinanceSentry.Modules.Alerts.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Alerts.Domain;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public class AlertGeneratorService(IAlertRepository alerts) : IAlertGeneratorService
{
    private static readonly TimeSpan LowBalanceSilenceWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan SyncFailureSilenceWindow = TimeSpan.FromHours(12);
    private static readonly TimeSpan UnusualSpendSilenceWindow = TimeSpan.FromDays(7);
    private static readonly TimeSpan ThesisBrokenSilenceWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan MarketStructureSilenceWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan MarketStructureFreshnessSilenceWindow = TimeSpan.FromHours(12);

    private readonly IAlertRepository _alerts = alerts;

    public async Task GenerateLowBalanceAlertAsync(
        Guid userId, Guid accountId, string accountName,
        decimal balance, decimal threshold, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.LowBalance, accountId, ct);
        if (existing is not null) return;

        var quietSince = DateTimeOffset.UtcNow - LowBalanceSilenceWindow;
        if (await _alerts.HasRecentAsync(userId, AlertType.LowBalance, accountId, accountName, quietSince, ct))
            return;

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

        var quietSince = DateTimeOffset.UtcNow - SyncFailureSilenceWindow;
        if (await _alerts.HasRecentAsync(userId, AlertType.SyncFailure, accountId, accountName, quietSince, ct))
            return;

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

        var quietSince = DateTimeOffset.UtcNow - UnusualSpendSilenceWindow;
        if (await _alerts.HasRecentAsync(userId, AlertType.UnusualSpend, null, category, quietSince, ct))
            return;

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

    public async Task GenerateThesisBreakAlertAsync(
        Guid userId, Guid thesisId, string ticker, string reason, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.ThesisBroken, thesisId, ct);
        if (existing is not null) return;

        var quietSince = DateTimeOffset.UtcNow - ThesisBrokenSilenceWindow;
        if (await _alerts.HasRecentAsync(userId, AlertType.ThesisBroken, thesisId, ticker, quietSince, ct))
            return;

        await _alerts.AddAsync(new Alert
        {
            UserId = userId,
            Type = AlertType.ThesisBroken,
            Severity = AlertSeverity.Warning,
            Title = $"Thesis broken: {ticker}",
            Message = $"Your investment thesis on {ticker} appears broken: {reason}",
            ReferenceId = thesisId,
            ReferenceLabel = ticker,
        }, ct);
    }

    public async Task ResolveThesisBreakAlertAsync(
        Guid userId, Guid thesisId, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.ThesisBroken, thesisId, ct);
        if (existing is null) return;
        await _alerts.ResolveAsync(existing.Id, ct);
    }

    public async Task GenerateMarketStructureAlertAsync(
        Guid userId, Guid referenceId, string ticker, string reason, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.MarketStructure, referenceId, ct);
        if (existing is not null) return;

        var quietSince = DateTimeOffset.UtcNow - MarketStructureSilenceWindow;
        if (await _alerts.HasRecentAsync(userId, AlertType.MarketStructure, referenceId, ticker, quietSince, ct))
            return;

        await _alerts.AddAsync(new Alert
        {
            UserId = userId,
            Type = AlertType.MarketStructure,
            Severity = AlertSeverity.Warning,
            Title = $"Unusual move: {ticker}",
            Message = $"Market structure flagged {ticker}: {reason}",
            ReferenceId = referenceId,
            ReferenceLabel = ticker,
        }, ct);
    }

    public async Task GenerateMarketStructureFreshnessAlertAsync(
        Guid userId, Guid referenceId, string reason, CancellationToken ct = default)
    {
        var existing = await _alerts.FindActiveAsync(userId, AlertType.MarketStructure, referenceId, ct);
        if (existing is not null) return;

        var quietSince = DateTimeOffset.UtcNow - MarketStructureFreshnessSilenceWindow;
        if (await _alerts.HasRecentAsync(userId, AlertType.MarketStructure, referenceId, "freshness", quietSince, ct))
            return;

        await _alerts.AddAsync(new Alert
        {
            UserId = userId,
            Type = AlertType.MarketStructure,
            Severity = AlertSeverity.Error,
            Title = "Radar data is stale",
            Message = $"Market-structure data may be unreliable: {reason}",
            ReferenceId = referenceId,
            ReferenceLabel = "freshness",
        }, ct);
    }
}
