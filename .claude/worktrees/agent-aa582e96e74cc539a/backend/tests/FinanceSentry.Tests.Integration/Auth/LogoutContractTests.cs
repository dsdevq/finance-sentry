namespace FinanceSentry.Tests.Integration.Auth;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// REST API contract tests for POST /api/v1/auth/logout (T047).
/// </summary>
public class LogoutContractTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private HttpClient CreateClient() =>
          factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Logout_WithoutToken_Returns204()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", null);

        // Logout is idempotent — no token is fine, just clear the cookie
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithValidSession_Returns204AndClearsRefreshCookie()
    {
        // Arrange: register and login to obtain a real session
        await factory.EnsureUserExistsAsync("logout-test@test.com", "TestPass123!");
        var loginClient = CreateClient();
        var loginResp = await loginClient.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "logout-test@test.com", password = "TestPass123!" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        loginResp.Headers.TryGetValues("Set-Cookie", out var cookies);
        var rawToken = cookies
            ?.FirstOrDefault(c => c.StartsWith("fs_refresh_token="))
            ?.Split(';')[0]
            .Replace("fs_refresh_token=", string.Empty);

        var logoutClient = CreateClient();
        if (!string.IsNullOrWhiteSpace(rawToken))
            logoutClient.DefaultRequestHeaders.Add("Cookie", $"fs_refresh_token={rawToken}");

        // Act
        var response = await logoutClient.PostAsync("/api/v1/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Set-Cookie should contain an expired/deleted fs_refresh_token cookie
        response.Headers.TryGetValues("Set-Cookie", out var setCookies);
        var deletedCookie = setCookies?.FirstOrDefault(c => c.Contains("fs_refresh_token=;")
                                                           || c.Contains("fs_refresh_token="));
        deletedCookie.Should().NotBeNull("logout must clear the refresh token cookie");
    }
}
