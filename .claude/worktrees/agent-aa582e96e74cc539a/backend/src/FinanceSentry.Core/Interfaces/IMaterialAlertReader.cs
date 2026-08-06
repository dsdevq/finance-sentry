namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Cross-module read contract (feature 031): lets the Companion module read newly-raised alerts
/// without referencing the Alerts module concretely. Implemented by the Alerts module.
/// </summary>
public interface IMaterialAlertReader
{
    /// <summary>Active (non-dismissed) alerts created after <paramref name="watermark"/>, oldest first.</summary>
    Task<IReadOnlyList<MaterialAlertRecord>> GetNewSinceAsync(
        DateTimeOffset watermark, int limit, CancellationToken ct = default);
}

/// <summary>A lightweight projection of an alert for companion capture.</summary>
public sealed record MaterialAlertRecord(
    Guid AlertId,
    Guid UserId,
    string Type,
    string Severity,
    string Title,
    Guid? ReferenceId,
    string? ReferenceLabel,
    DateTimeOffset CreatedAt);
