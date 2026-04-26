using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace FinanceSentry.Tests.Integration.BrokerageSync;

// ── Contract tests: POST /api/v1/brokerage/ibkr/connect ──────────────────────
//
// Single-tenant model: the IBeam sidecar owns the IBKR session. The endpoint
// accepts no body — it just verifies the gateway is authenticated, discovers
// the account, and stores the link metadata.

public class BrokerageControllerConnectContractTests(BrokerageApiFactory factory) : IClassFixture<BrokerageApiFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();
    private readonly BrokerageApiFactory _factory = factory;

    [Fact]
    public async Task Connect_NoAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsync("/api/v1/brokerage/ibkr/connect", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Connect_GatewaySessionNotAuthenticated_Returns422()
    {
        _factory.CredentialRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null);

        _factory.AdapterMock
            .Setup(a => a.EnsureSessionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BrokerAuthException("not authenticated", "IBKR"));

        var response = await _client.PostAsync("/api/v1/brokerage/ibkr/connect", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<BrokerageErrorShape>();
        body!.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Connect_GatewayAuthenticated_Returns201WithShape()
    {
        _factory.SetupSuccessfulConnect();

        var response = await _client.PostAsync("/api/v1/brokerage/ibkr/connect", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BrokerageConnectResponseShape>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be("U1234567");
        body.HoldingsCount.Should().BeGreaterThanOrEqualTo(0);
        body.ConnectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Connect_AlreadyConnected_Returns409()
    {
        var existingCredential = new IBKRCredential(_factory.TestUserId, "U1234567");

        _factory.CredentialRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCredential);

        var response = await _client.PostAsync("/api/v1/brokerage/ibkr/connect", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<BrokerageErrorShape>();
        body!.ErrorCode.Should().Be("ALREADY_CONNECTED");
    }
}

// ── Response shapes ───────────────────────────────────────────────────────────

public record BrokerageErrorShape(string Error, string ErrorCode);
public record BrokerageConnectResponseShape(int HoldingsCount, DateTime ConnectedAt, string AccountId);
public record BrokeragePositionShape(string Symbol, string InstrumentType, decimal Quantity, decimal UsdValue);
public record BrokerageHoldingsResponseShape(
    string Provider,
    DateTime? SyncedAt,
    bool IsStale,
    List<BrokeragePositionShape> Positions,
    decimal TotalUsdValue);

// ── Shared WebApplicationFactory ─────────────────────────────────────────────

public class BrokerageApiFactory : WebApplicationFactory<Program>
{
    public Mock<IIBKRCredentialRepository> CredentialRepoMock { get; } = new(MockBehavior.Loose);
    public Mock<IBrokerageHoldingRepository> HoldingRepoMock { get; } = new(MockBehavior.Loose);
    public Mock<IBrokerAdapter> AdapterMock { get; } = new(MockBehavior.Loose);

    public Guid TestUserId { get; } = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceService(services, CredentialRepoMock.Object);
            ReplaceService(services, HoldingRepoMock.Object);
            ReplaceService<IBrokerAdapter>(services, AdapterMock.Object);

            ReplaceDbContextWithInMemory<FinanceSentry.Modules.BankSync.Infrastructure.Persistence.BankSyncDbContext>(
                services, $"BrokerageTestBankSync_{Guid.NewGuid()}");
            ReplaceDbContextWithInMemory<FinanceSentry.Modules.Auth.Infrastructure.Persistence.AuthDbContext>(
                services, $"BrokerageTestAuth_{Guid.NewGuid()}");
            ReplaceDbContextWithInMemory<FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence.CryptoSyncDbContext>(
                services, $"BrokerageTestCrypto_{Guid.NewGuid()}");
            ReplaceDbContextWithInMemory<BrokerageSyncDbContext>(
                services, $"BrokerageTestBrokerage_{Guid.NewGuid()}");
        });

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default",
            "Host=localhost;Database=test;Username=test;Password=test");
        builder.UseSetting("Deduplication:MasterKeyBase64",
            "dGVzdGtleS10ZXN0a2V5LXRlc3RrZXktdGVzdGtleTA=");
        builder.UseSetting("Encryption:CurrentKeyVersion", "1");
        builder.UseSetting("Encryption:Keys:1",
            "dGVzdGtleS10ZXN0a2V5LXRlc3RrZXktdGVzdGtleTA=");
        builder.UseSetting("Plaid:ClientId", "test-client-id");
        builder.UseSetting("Plaid:Secret", "test-secret");
        builder.UseSetting("Jwt:Secret",
            "test-jwt-secret-key-for-integration-tests-minimum-32-chars");
        builder.UseSetting("Binance:BaseUrl", "https://testnet.binance.vision");
        builder.UseSetting("Binance:DustThresholdUsd", "0.01");
        builder.UseSetting("IBKR:GatewayBaseUrl", "http://localhost:9999");
    }

    public void SetupSuccessfulConnect()
    {
        var mockCredential = new IBKRCredential(TestUserId, "U1234567");

        CredentialRepoMock
            .SetupSequence(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IBKRCredential?)null)
            .ReturnsAsync(mockCredential);

        CredentialRepoMock
            .Setup(r => r.AddAsync(It.IsAny<IBKRCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        CredentialRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        CredentialRepoMock
            .Setup(r => r.Update(It.IsAny<IBKRCredential>()));

        AdapterMock
            .Setup(a => a.EnsureSessionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        AdapterMock
            .Setup(a => a.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        AdapterMock
            .Setup(a => a.GetPositionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BrokerPosition>)[]);
        AdapterMock
            .Setup(a => a.BrokerName)
            .Returns("IBKR");

        HoldingRepoMock
            .Setup(r => r.UpsertRangeAsync(
                It.IsAny<IEnumerable<BrokerageHolding>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        HoldingRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestJwt(TestUserId));
        return client;
    }

    private static string GenerateTestJwt(Guid userId)
    {
        const string secret = "test-jwt-secret-key-for-integration-tests-minimum-32-chars";
        var key = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(secret));
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", userId.ToString())]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
        });
        return handler.WriteToken(token);
    }

    private static void ReplaceService<T>(IServiceCollection services, T implementation)
        where T : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
        services.AddScoped(_ => implementation);
    }

    private static void ReplaceDbContextWithInMemory<TContext>(IServiceCollection services, string dbName)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>)
                     || d.ServiceType == typeof(TContext)
                     || d.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>))
            .ToList();
        foreach (var d in toRemove)
            services.Remove(d);

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }
}
