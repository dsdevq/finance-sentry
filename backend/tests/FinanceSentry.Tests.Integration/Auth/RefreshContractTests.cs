namespace FinanceSentry.Tests.Integration.Auth;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// REST API contract tests for POST /api/v1/auth/refresh (T046).
/// </summary>
public class RefreshContractTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private HttpClient CreateClient() =>
          factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Refresh_WithNoCookie_Returns401WithInvalidRefreshToken()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/v1/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithInvalidCookieValue_Returns401WithInvalidRefreshToken()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "fs_refresh_token=not-a-real-token");

        var response = await client.PostAsync("/api/v1/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_HappyPath_Returns200WithAuthResponseSchemaAndNewCookie()
    {
        // Arrange: register a user to obtain a real refresh token cookie
        await factory.EnsureUserExistsAsync("refresh-happy@test.com", "TestPass123!");
        var loginClient = CreateClient();
        var loginResp = await loginClient.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "refresh-happy@test.com", password = "TestPass123!" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Extract the refresh token cookie set during login
        loginResp.Headers.TryGetValues("Set-Cookie", out var cookies);
        var refreshCookie = cookies?.FirstOrDefault(c => c.StartsWith("fs_refresh_token="));
        refreshCookie.Should().NotBeNullOrWhiteSpace("login must set the refresh token cookie");

        var rawToken = refreshCookie!
            .Split(';')[0]
            .Replace("fs_refresh_token=", string.Empty);

        // Act: call refresh using the raw token as a Cookie header
        var refreshClient = CreateClient();
        refreshClient.DefaultRequestHeaders.Add("Cookie", $"fs_refresh_token={rawToken}");

        var response = await refreshClient.PostAsync("/api/v1/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseShape>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        body.UserId.Should().NotBeNullOrWhiteSpace();

        response.Headers.TryGetValues("Set-Cookie", out var newCookies);
        newCookies?.Any(c => c.StartsWith("fs_refresh_token=")).Should().BeTrue("refresh must rotate the cookie");
    }

    private record AuthResponseShape(string Token, DateTime ExpiresAt, string UserId);
    private record ErrorResponseShape(string Error, string ErrorCode);
}
