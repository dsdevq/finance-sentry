namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

/// <summary>
/// IPlaidClient implementation that calls the real Plaid REST API over HTTPS.
/// Injected via AddHttpClient in Program.cs; BaseAddress set from config.
/// </summary>
public class PlaidHttpClient : IPlaidClient
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _secret;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PlaidHttpClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _clientId = config["Plaid:ClientId"] ?? throw new InvalidOperationException("Plaid:ClientId is required.");
        _secret = config["Plaid:Secret"] ?? throw new InvalidOperationException("Plaid:Secret is required.");
    }

    public async Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(string userId, CancellationToken ct = default)
    {
        var body = new
        {
            client_id = _clientId,
            secret = _secret,
            user = new { client_user_id = userId },
            client_name = "Finance Sentry",
            products = new[] { "transactions" },
            country_codes = new[] { "IE", "UA", "GB", "US" },
            language = "en"
        };

        var response = await PostAsync<PlaidLinkTokenRaw>("/link/token/create", body, ct);
        return new PlaidLinkTokenResponse(response.LinkToken, response.RequestId,
            DateTime.UtcNow.AddMinutes(30));
    }

    public async Task<PlaidExchangeTokenResponse> ExchangePublicTokenAsync(string publicToken, CancellationToken ct = default)
    {
        var body = new { client_id = _clientId, secret = _secret, public_token = publicToken };
        var response = await PostAsync<PlaidExchangeRaw>("/item/public_token/exchange", body, ct);
        return new PlaidExchangeTokenResponse(response.AccessToken, response.ItemId, response.RequestId);
    }

    public async Task<PlaidAccountsResponse> GetAccountsAsync(string accessToken, CancellationToken ct = default)
    {
        var body = new { client_id = _clientId, secret = _secret, access_token = accessToken };
        var response = await PostAsync<PlaidAccountsRaw>("/accounts/balance/get", body, ct);
        var accounts = response.Accounts.Select(a => new PlaidAccount(
            a.AccountId, a.Name, a.OfficialName ?? a.Name,
            a.Type, a.Subtype ?? a.Type, a.Mask ?? "0000",
            a.Balances?.Current, a.Balances?.Available,
            a.Balances?.IsoCurrencyCode ?? "EUR")).ToList();
        return new PlaidAccountsResponse(accounts, response.RequestId);
    }

    public async Task<PlaidTransactionsResponse> GetTransactionsAsync(
        string accessToken, DateTime startDate, DateTime endDate,
        int offset = 0, int count = 500, CancellationToken ct = default)
    {
        var body = new
        {
            client_id = _clientId,
            secret = _secret,
            access_token = accessToken,
            start_date = startDate.ToString("yyyy-MM-dd"),
            end_date = endDate.ToString("yyyy-MM-dd"),
            options = new { offset, count }
        };
        var response = await PostAsync<PlaidTransactionsRaw>("/transactions/get", body, ct);
        var txns = response.Transactions.Select(t => new PlaidTransaction(
            t.TransactionId, t.AccountId, t.Amount,
            t.IsoCurrencyCode, t.Name, t.MerchantName,
            t.PersonalFinanceCategory?.Primary,
            DateTime.Parse(t.Date),
            t.AuthorizedDate != null ? DateTime.Parse(t.AuthorizedDate) : null,
            t.Pending)).ToList();
        return new PlaidTransactionsResponse(txns, response.TotalTransactions, response.RequestId);
    }

    public async Task RevokeAccessAsync(string accessToken, CancellationToken ct = default)
    {
        var body = new { client_id = _clientId, secret = _secret, access_token = accessToken };
        await PostAsync<PlaidRevokeRaw>("/item/remove", body, ct);
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOpts, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<PlaidErrorRaw>(JsonOpts, ct);
            throw new PlaidException(
                (int)response.StatusCode,
                err?.ErrorCode ?? "UNKNOWN",
                err?.ErrorMessage ?? response.ReasonPhrase ?? "Unknown Plaid error");
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct))!;
    }

    // ── Raw Plaid JSON shapes (snake_case from API) ──────────────────────────

    private record PlaidLinkTokenRaw(
        [property: JsonPropertyName("link_token")] string LinkToken,
        [property: JsonPropertyName("request_id")] string RequestId);

    private record PlaidExchangeRaw(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("item_id")] string ItemId,
        [property: JsonPropertyName("request_id")] string RequestId);

    private record PlaidAccountsRaw(
        [property: JsonPropertyName("accounts")] List<PlaidAccountRaw> Accounts,
        [property: JsonPropertyName("request_id")] string RequestId);

    private record PlaidAccountRaw(
        [property: JsonPropertyName("account_id")] string AccountId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("official_name")] string? OfficialName,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("subtype")] string? Subtype,
        [property: JsonPropertyName("mask")] string? Mask,
        [property: JsonPropertyName("balances")] PlaidBalanceRaw? Balances);

    private record PlaidBalanceRaw(
        [property: JsonPropertyName("current")] decimal? Current,
        [property: JsonPropertyName("available")] decimal? Available,
        [property: JsonPropertyName("iso_currency_code")] string? IsoCurrencyCode);

    private record PlaidTransactionsRaw(
        [property: JsonPropertyName("transactions")] List<PlaidTransactionRaw> Transactions,
        [property: JsonPropertyName("total_transactions")] int TotalTransactions,
        [property: JsonPropertyName("request_id")] string RequestId);

    private record PlaidTransactionRaw(
        [property: JsonPropertyName("transaction_id")] string TransactionId,
        [property: JsonPropertyName("account_id")] string AccountId,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("iso_currency_code")] string? IsoCurrencyCode,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("merchant_name")] string? MerchantName,
        [property: JsonPropertyName("personal_finance_category")] PlaidCategoryRaw? PersonalFinanceCategory,
        [property: JsonPropertyName("date")] string Date,
        [property: JsonPropertyName("authorized_date")] string? AuthorizedDate,
        [property: JsonPropertyName("pending")] bool Pending);

    private record PlaidCategoryRaw(
        [property: JsonPropertyName("primary")] string Primary);

    private record PlaidRevokeRaw(
        [property: JsonPropertyName("request_id")] string RequestId);

    private record PlaidErrorRaw(
        [property: JsonPropertyName("error_code")] string? ErrorCode,
        [property: JsonPropertyName("error_message")] string? ErrorMessage);
}
