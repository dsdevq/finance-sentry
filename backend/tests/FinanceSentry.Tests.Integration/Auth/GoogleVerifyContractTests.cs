namespace FinanceSentry.Tests.Integration.Auth;

using System.Net;
using System.Net.Http.Json;
using FinanceSentry.Modules.Auth.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// REST API contract tests for POST /api/v1/auth/google/verify (T022).
/// Validates response shapes and status codes per contracts/google-oauth.md.
/// </summary>
public class GoogleVerifyContractTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public GoogleVerifyContractTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GoogleVerify_ValidCredential_Returns200WithAuthResponseSchema()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "valid-test-credential" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseShape>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        body.UserId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(body.UserId, out _).Should().BeTrue("UserId must be a valid GUID");
    }

    [Fact]
    public async Task GoogleVerify_EmptyCredential_Returns400WithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task GoogleVerify_InvalidCredential_Returns400WithInvalidGoogleCredential()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "invalid-credential" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("INVALID_GOOGLE_CREDENTIAL");
    }

    [Fact]
    public async Task GoogleVerify_NewUser_Returns200AndCreatesUserInDb()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "new-user-credential" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseShape>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("newgoogle@test.com");
        user.Should().NotBeNull("new user must be created in DB");
        user!.GoogleId.Should().Be("google-sub-new");
    }

    [Fact]
    public async Task GoogleVerify_ExistingGoogleUser_Returns200WithSameUserId()
    {
        // First call creates the user
        var first = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "valid-test-credential" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<AuthResponseShape>();

        // Second call returns same user
        var second = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "valid-test-credential" });
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<AuthResponseShape>();

        secondBody!.UserId.Should().Be(firstBody!.UserId, "same Google account must always resolve to same user");
    }

    [Fact]
    public async Task GoogleVerify_EmailMatchesExistingAccount_Returns200AndLinksGoogleId()
    {
        await _factory.EnsureUserExistsAsync("link@test.com", "TestPass123!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/verify",
            new { credential = "link-credential" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("link@test.com");
        user.Should().NotBeNull();
        user!.GoogleId.Should().Be("google-sub-link", "GoogleId must be linked to existing account");
    }

    private record AuthResponseShape(string Token, DateTime ExpiresAt, string UserId);
    private record ErrorResponseShape(string Error, string ErrorCode);
}
