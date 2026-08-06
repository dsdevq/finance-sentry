namespace FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;

public interface ITrueLayerClient
{
    Task<IReadOnlyList<TrueLayerProvider>> ListProvidersAsync(string? country, CancellationToken ct = default);

    string BuildAuthLink(string providerId, string reference, string redirectUri);

    Task<TrueLayerTokenSet> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);

    Task<TrueLayerTokenSet> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);

    Task<IReadOnlyList<TrueLayerAccountInfo>> ListAccountsAsync(string accessToken, CancellationToken ct = default);

    Task<TrueLayerAccountBalance?> GetBalanceAsync(string accessToken, string accountId, CancellationToken ct = default);

    Task<IReadOnlyList<TrueLayerTransaction>> GetTransactionsAsync(
        string accessToken,
        string accountId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct = default);

    Task<IReadOnlyList<TrueLayerTransaction>> GetPendingTransactionsAsync(
        string accessToken,
        string accountId,
        CancellationToken ct = default);
}
