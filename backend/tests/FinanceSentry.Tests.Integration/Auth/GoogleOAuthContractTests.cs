namespace FinanceSentry.Tests.Integration.Auth;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class GoogleOAuthContractTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public GoogleOAuthContractTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GoogleLogin_Returns302_ToGoogleConsentScreen()
    {
        var response = await _client.GetAsync("/api/v1/auth/google/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth");
        response.Headers.Location.Query.Should().Contain("state=");
    }

    [Fact]
    public async Task GoogleCallback_WithErrorParam_Returns302_ToCancelledUrl()
    {
        var response = await _client.GetAsync(
            "/api/v1/auth/google/callback?error=access_denied&state=any");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should().Be("http://localhost:4200/auth/callback?error=cancelled");
    }

    [Fact]
    public async Task GoogleCallback_WithNoState_Returns400_InvalidOAuthState()
    {
        var response = await _client.GetAsync("/api/v1/auth/google/callback?code=somecode");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("INVALID_OAUTH_STATE");
    }

    [Fact]
    public async Task GoogleCallback_WithUnknownState_Returns400_InvalidOAuthState()
    {
        var response = await _client.GetAsync(
            "/api/v1/auth/google/callback?code=somecode&state=nonexistent-state");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("INVALID_OAUTH_STATE");
    }

    private record ErrorShape(string Error, string ErrorCode);
}
