namespace FinanceSentry.Tests.Integration.Auth;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// REST API contract tests for POST /api/v1/auth/register (T019).
/// Validates response shapes and status codes per contracts/auth-api.md.
/// </summary>
public class RegisterContractTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public RegisterContractTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_HappyPath_Returns201WithAuthResponseSchema()
    {
        var email = $"reg-{Guid.NewGuid():N}@test.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "TestPass123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseShape>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        body.UserId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(body.UserId, out _).Should().BeTrue("UserId must be a valid GUID");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400WithDuplicateEmail()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        await _factory.EnsureUserExistsAsync(email, "TestPass123!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "TestPass123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("DUPLICATE_EMAIL");
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400WithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "", password = "TestPass123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400WithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"weak-{Guid.NewGuid():N}@test.com", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseShape>();
        body.Should().NotBeNull();
        body!.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    private record AuthResponseShape(string Token, DateTime ExpiresAt, string UserId);
    private record ErrorResponseShape(string Error, string ErrorCode, string[]? Details);
}
