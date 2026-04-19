namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

/// <summary>
/// IPlaidClient implementation that calls the real Plaid REST API over HTTPS.
/// Injected via AddHttpClient in Program.cs; BaseAddress set from config.
/// </summary>
public class PlaidHttpClient(HttpClient http, IConfiguration config) : IPlaidClient
{
    private readonly HttpClient _http = http;
    private readonly string _clientId = config["Plaid:ClientId"] ?? throw new InvalidOperationException("Plaid:ClientId is required.");
    private readonly string _secret = config["Plaid:Secret"] ?? throw new InvalidOperationException("Plaid:Secret is required.");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(string userId, CancellationToken ct = default)
    {
        var body = new
        {
            client_id = _clientId,
            secret = _secret,
            user = new { client_user_id = userId },
            client_name = "Finance Sentry",
            products = new[] { "transactions" },
            country_codes = new[] { "US", "IE" },
            language = "en"
        };

        var response = await PostAsync<PlaidLinkTokenRaw>("/link/token/create", body, ct);
        var expiration = DateTime.Parse(response.Expiration, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return new PlaidLinkTokenResponse(response.LinkToken, response.RequestId, expiration);
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
            a.Balances?.IsoCurrencyCode ?? "USD")).ToList();
        return new PlaidAccountsResponse(accounts, response.RequestId);
    }

    /// <summary>
    /// Fetches all incremental changes via /transactions/sync.
    /// Paginates automatically until has_more = false, collecting all added/modified/removed.
    /// Pass null cursor for the initial full sync.
    /// </summary>
    public async Task<PlaidSyncResponse> SyncTransactionsAsync(
        string accessToken, string? cursor = null, int count = 500, CancellationToken ct = default)
    {
        var allAdded = new List<PlaidTransaction>();
        var allModified = new List<PlaidTransaction>();
        var allRemoved = new List<string>();
        var nextCursor = cursor ?? string.Empty;
        string requestId = string.Empty;

        bool hasMore;
        do
        {
            var body = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken,
                cursor = string.IsNullOrEmpty(nextCursor) ? null : nextCursor,
                count
            };

            var page = await PostAsync<PlaidSyncRaw>("/transactions/sync", body, ct);
            requestId = page.RequestId;
            nextCursor = page.NextCursor;
            hasMore = page.HasMore;

            allAdded.AddRange(page.Added.Select(MapTransaction));
            allModified.AddRange(page.Modified.Select(MapTransaction));
            allRemoved.AddRange(page.Removed.Select(r => r.TransactionId));
        }
        while (hasMore);

        return new PlaidSyncResponse(allAdded, allModified, allRemoved, nextCursor, false, requestId);
    }

    public async Task RevokeAccessAsync(string accessToken, CancellationToken ct = default)
    {
        var body = new { client_id = _clientId, secret = _secret, access_token = accessToken };
        await PostAsync<PlaidRevokeRaw>("/item/remove", body, ct);
    }

    private static PlaidTransaction MapTransaction(PlaidTransactionRaw t) => new(
        t.TransactionId, t.AccountId, t.Amount,
        t.IsoCurrencyCode, t.Name, t.MerchantName,
        t.PersonalFinanceCategory?.Primary,
        DateTime.Parse(t.Date, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
        t.AuthorizedDate != null ? DateTime.Parse(t.AuthorizedDate, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal) : null,
        t.Pending);

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

    // ── Raw Plaid JSON shapes ────────────────────────────────────────────────

    private record PlaidLinkTokenRaw(
        [property: JsonPropertyName("link_token")] string LinkToken,
        [property: JsonPropertyName("expiration")] string Expiration,
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

    private record PlaidSyncRaw(
        [property: JsonPropertyName("added")] List<PlaidTransactionRaw> Added,
        [property: JsonPropertyName("modified")] List<PlaidTransactionRaw> Modified,
        [property: JsonPropertyName("removed")] List<PlaidRemovedRaw> Removed,
        [property: JsonPropertyName("next_cursor")] string NextCursor,
        [property: JsonPropertyName("has_more")] bool HasMore,
        [property: JsonPropertyName("request_id")] string RequestId);

    private record PlaidRemovedRaw(
        [property: JsonPropertyName("transaction_id")] string TransactionId);

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
