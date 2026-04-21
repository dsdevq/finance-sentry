namespace FinanceSentry.Modules.BankSync.Infrastructure.Monobank;

public interface IMonobankAdapter
{
    Task<IReadOnlyList<MonobankAccountInfo>> ConnectAsync(string token, CancellationToken ct = default);

    Task<IReadOnlyList<MonobankAccountInfo>> GetAccountsAsync(string token, CancellationToken ct = default);

    Task<IReadOnlyList<MonobankTransaction>> GetStatementsAsync(
        string token, string accountId, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct = default);

    Task SetWebhookAsync(string token, string url, CancellationToken ct = default);
}
