namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

/// <summary>
/// The cache-invalidation rule for "Ledger's read" (feature 421, US3): a cached narrative goes
/// stale a day after it was written, or as soon as the dossier facts behind it move.
/// </summary>
public static class LedgerReadStaleness
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    /// <param name="currentFingerprint">
    /// Fingerprint of the dossier as it stands now, or null when it could not be computed — in
    /// which case only age is considered.
    /// </param>
    public static bool IsStale(AssetLedgerRead read, string? currentFingerprint) =>
        DateTimeOffset.UtcNow - read.GeneratedAt >= MaxAge
        || (currentFingerprint is not null && currentFingerprint != read.SourceFingerprint);
}
