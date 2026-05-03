namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank.History;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using Microsoft.Extensions.Logging;

public sealed class MonobankHistorySource(
    IMonobankCredentialRepository credentialRepository,
    IBankAccountRepository bankAccountRepository,
    MonobankHttpClient httpClient,
    ICredentialEncryptionService encryption,
    ILogger<MonobankHistorySource> logger) : IProviderMonthlyHistorySource
{
    public async Task<IReadOnlyList<ProviderMonthlyBalance>> GetMonthlyBalancesAsync(
        Guid userId, CancellationToken ct = default)
    {
        var credential = await credentialRepository.GetByUserIdAsync(userId, ct);
        if (credential is null)
            return [];

        string token;
        try
        {
            token = encryption.Decrypt(credential.EncryptedToken, credential.Iv, credential.AuthTag, credential.KeyVersion);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Monobank token decryption failed for user {UserId} during backfill — returning empty", userId);
            return [];
        }

        var allAccounts = await bankAccountRepository.GetByUserIdAsync(userId, ct);
        var monobankAccounts = allAccounts
            .Where(a => string.Equals(a.Provider, "monobank", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (monobankAccounts.Count == 0)
            return [];

        var monthlyTotals = new Dictionary<DateOnly, decimal>();
        var now = DateTimeOffset.UtcNow;

        foreach (var account in monobankAccounts)
        {
            var windowEnd = now;
            var isFirstWindow = true;

            while (windowEnd > now.AddYears(-1))
            {
                if (!isFirstWindow)
                    await Task.Delay(TimeSpan.FromSeconds(60), ct);
                isFirstWindow = false;

                var windowStart = windowEnd.AddDays(-31);

                IReadOnlyList<MonobankTransaction> statements;
                try
                {
                    statements = await httpClient.GetStatementsAsync(token, account.ExternalAccountId, windowStart, windowEnd, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Monobank statement fetch failed for account {AccountId} user {UserId} — stopping window chain", account.ExternalAccountId, userId);
                    break;
                }

                if (statements.Count == 0)
                    break;

                foreach (var txGroup in statements.GroupBy(tx => MonthEnd(DateTimeOffset.FromUnixTimeSeconds(tx.Time).UtcDateTime)))
                {
                    var lastTx = txGroup.OrderBy(tx => tx.Time).Last();
                    var amountDecimal = MonobankHttpClient.KopecksToDecimal(lastTx.Balance);
                    var amountUsd = CurrencyConverter.ToUsd(amountDecimal, account.Currency);

                    if (monthlyTotals.TryGetValue(txGroup.Key, out var existing))
                        monthlyTotals[txGroup.Key] = existing + amountUsd;
                    else
                        monthlyTotals[txGroup.Key] = amountUsd;
                }

                windowEnd = windowStart;
            }
        }

        return monthlyTotals
            .Select(kvp => new ProviderMonthlyBalance(kvp.Key, kvp.Value, "banking"))
            .ToList();
    }

    private static DateOnly MonthEnd(DateTime d)
    {
        var daysInMonth = DateTime.DaysInMonth(d.Year, d.Month);
        return new DateOnly(d.Year, d.Month, daysInMonth);
    }
}
