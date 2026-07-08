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

    Task GenerateThesisBreakAlertAsync(
        Guid userId,
        Guid thesisId,
        string ticker,
        string reason,
        CancellationToken ct = default);

    Task ResolveThesisBreakAlertAsync(
        Guid userId,
        Guid thesisId,
        CancellationToken ct = default);

    /// <summary>
    /// Raises a market-structure Alert for a held ticker (e.g. an unusual move at/above the alert bar).
    /// <paramref name="referenceId"/> is a deterministic per-ticker id so dedup/resolve is stable.
    /// </summary>
    Task GenerateMarketStructureAlertAsync(
        Guid userId,
        Guid referenceId,
        string ticker,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Raises a market-structure freshness Alert when the Radar data is stale or an ingestion run failed.
    /// </summary>
    Task GenerateMarketStructureFreshnessAlertAsync(
        Guid userId,
        Guid referenceId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Raises a policy-violation Alert (022) naming the rule, observed value, and limit. When
    /// <paramref name="isOverride"/> is true, this records an explicit override of a Refused
    /// verdict rather than a fresh violation (FR-007) — always Info severity, never silent.
    /// </summary>
    Task GeneratePolicyViolationAlertAsync(
        Guid userId,
        string ruleKey,
        string subject,
        decimal observedValue,
        decimal limitValue,
        bool isOverride = false,
        CancellationToken ct = default);

    Task ResolvePolicyViolationAlertAsync(
        Guid userId,
        string ruleKey,
        string subject,
        CancellationToken ct = default);
}
