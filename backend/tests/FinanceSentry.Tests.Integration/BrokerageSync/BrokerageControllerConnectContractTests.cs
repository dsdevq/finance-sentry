using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
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

// ── Contract tests: async IBKR connect flow ──────────────────────────────────
//
// POST /api/v1/brokerage/ibkr/connect returns 202 immediately with a
// { sessionId } body. GET /api/v1/brokerage/ibkr/connect/{sessionId} exposes
// the current state (Pending → Spawning → AwaitingAuth → Syncing → Completed
// | Failed | Cancelled). DELETE .../{sessionId} cancels an in-flight session.

public class BrokerageControllerConnectContractTests(BrokerageApiFactory factory) : IClassFixture<BrokerageApiFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();
    private readonly BrokerageApiFactory _factory = factory;

    [Fact]
    public async Task Connect_NoAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync(
            "/api/v1/brokerage/ibkr/connect",
            new { Username = "u", Password = "p" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Connect_Returns202_WithSessionId_ThenPollsToCompleted()
    {
        _factory.SetupSuccessfulConnect();

        var response = await _client.PostAsJsonAsync(
            "/api/v1/brokerage/ibkr/connect",
            new { Username = "u", Password = "p" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await response.Content.ReadFromJsonAsync<SessionAcceptedShape>();
        accepted!.SessionId.Should().NotBe(Guid.Empty);

        var final = await PollUntilTerminalAsync(accepted.SessionId);
        final.Status.Should().Be("completed");
        final.Result.Should().NotBeNull();

        _factory.ContainerManagerMock.Verify(
            m => m.SpawnAsync(It.IsAny<Guid>(), "u", "p", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Connect_AlreadyConnected_SessionTransitionsTo_FailedALREADY_CONNECTED()
    {
        _factory.SetupSuccessfulConnect();
        var existing = new IBKRCredential(_factory.TestUserId, [1], [2], [3], [4], [5], [6], keyVersion: 1);
        // Overwrite the sequence with a single "existing active" record so the
        // runner short-circuits into ALREADY_CONNECTED.
        _factory.CredentialRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/brokerage/ibkr/connect",
            new { Username = "u", Password = "p" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await response.Content.ReadFromJsonAsync<SessionAcceptedShape>();

        var final = await PollUntilTerminalAsync(accepted!.SessionId);
        final.Status.Should().Be("failed");
        final.ErrorCode.Should().Be("IBKR_DUPLICATE");
    }

    [Fact]
    public async Task GetConnectStatus_UnknownSession_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/brokerage/ibkr/connect/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelConnect_UnknownSession_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/v1/brokerage/ibkr/connect/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<SessionShape> PollUntilTerminalAsync(Guid sessionId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var resp = await _client.GetAsync($"/api/v1/brokerage/ibkr/connect/{sessionId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = (await resp.Content.ReadFromJsonAsync<SessionShape>())!;
            if (body.Status is "completed" or "failed" or "cancelled")
                return body;
            await Task.Delay(50);
        }
        throw new Xunit.Sdk.XunitException($"Session {sessionId} did not reach terminal state within 5s");
    }
}

// ── Response shapes ───────────────────────────────────────────────────────────

public record BrokerageErrorShape(string Error, string ErrorCode);
public record SessionAcceptedShape(Guid SessionId);
public record SessionShape(
    Guid SessionId,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    ResultShape? Result,
    DateTime CreatedAt,
    DateTime UpdatedAt);
public record ResultShape(int HoldingsCount, DateTime ConnectedAt, string AccountId);
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
    public Mock<IIBeamContainerManager> ContainerManagerMock { get; } = new(MockBehavior.Loose);

    public Guid TestUserId { get; } = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceService(services, CredentialRepoMock.Object);
            ReplaceService(services, HoldingRepoMock.Object);
            ReplaceService<IBrokerAdapter>(services, AdapterMock.Object);
            ReplaceService(services, ContainerManagerMock.Object);

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
        ContainerManagerMock
            .Setup(m => m.SpawnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ContainerManagerMock
            .Setup(m => m.WaitForAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        ContainerManagerMock
            .Setup(m => m.StopAndRemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockCredential = new IBKRCredential(TestUserId, [1], [2], [3], [4], [5], [6], keyVersion: 1);

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
            .Setup(a => a.EnsureSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        AdapterMock
            .Setup(a => a.GetAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("U1234567");
        AdapterMock
            .Setup(a => a.GetPositionsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        HoldingRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $"fs_access_token={GenerateTestJwt(TestUserId)}");
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
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
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
