namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Process-wide cache for Monobank account balances keyed by token-hash + externalAccountId.
/// The /personal/client-info endpoint is rate-limited to one call per 60 seconds per token,
/// so the BulkSyncMonobank command primes this once and the per-account syncs read from it.
/// Entries expire after the same window so stale values can't linger past the rate-limit cooldown.
/// </summary>
public sealed class MonobankBalanceCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(90);
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public void Set(string token, string externalAccountId, decimal balance)
    {
        _entries[BuildKey(token, externalAccountId)] = new Entry(balance, DateTime.UtcNow);
    }

    public decimal? TryGet(string token, string externalAccountId)
    {
        if (_entries.TryGetValue(BuildKey(token, externalAccountId), out var entry)
            && DateTime.UtcNow - entry.StoredAt < Ttl)
        {
            return entry.Balance;
        }
        return null;
    }

    private static string BuildKey(string token, string externalAccountId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes) + "|" + externalAccountId;
    }

    private readonly record struct Entry(decimal Balance, DateTime StoredAt);
}
