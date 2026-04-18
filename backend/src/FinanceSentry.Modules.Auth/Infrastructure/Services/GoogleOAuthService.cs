using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinanceSentry.Modules.Auth.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FinanceSentry.Modules.Auth.Infrastructure.Services;

public class GoogleOAuthService(IHttpClientFactory httpClientFactory, IOptions<GoogleOAuthOptions> options) : IGoogleOAuthService
{
    private readonly GoogleOAuthOptions _options = options.Value;

    public string GetAuthorizationUrl(string state)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = _options.ClientId;
        query["redirect_uri"] = _options.RedirectUri;
        query["response_type"] = "code";
        query["scope"] = "openid email profile";
        query["state"] = state;
        query["access_type"] = "offline";
        query["prompt"] = "consent";
        return $"https://accounts.google.com/o/oauth2/v2/auth?{query}";
    }

    public async Task<GoogleUserInfo> ExchangeCodeAsync(string code)
    {
        var client = httpClientFactory.CreateClient("google");

        var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = _options.RedirectUri,
                ["grant_type"] = "authorization_code"
            }));

        tokenResponse.EnsureSuccessStatusCode();
        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>()
            ?? throw new InvalidOperationException("Empty token response from Google.");

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);

        var userInfoResponse = await client.SendAsync(userInfoRequest);
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>()
            ?? throw new InvalidOperationException("Empty userinfo response from Google.");

        return new GoogleUserInfo(userInfo.Sub, userInfo.Email, userInfo.Name);
    }

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record GoogleUserInfoResponse(
        [property: JsonPropertyName("sub")] string Sub,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string? Name);
}
