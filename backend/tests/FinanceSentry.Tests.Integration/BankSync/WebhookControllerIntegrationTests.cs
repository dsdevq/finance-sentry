namespace FinanceSentry.Tests.Integration.BankSync;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Jobs;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using FinanceSentry.Modules.BankSync.Infrastructure.Security;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

/// <summary>
/// Integration tests for POST /api/v1/webhook/plaid.
/// Covers signature validation, webhook_type routing, and background job enqueueing.
/// </summary>
public class WebhookControllerIntegrationTests(WebhookApiFactory factory) : IClassFixture<WebhookApiFactory>
{
    private const string WebhookKey = "test-webhook-key-for-integration";
    private const string WebhookPath = "/api/v1/webhook/plaid";

    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private readonly WebhookApiFactory _factory = factory;

    [Fact]
    public async Task Post_InvalidSignature_Returns401()
    {
        _factory.Reset();

        const string body = """{"webhook_type":"TRANSACTIONS","webhook_code":"TRANSACTIONS_READY","item_id":"item_1"}""";

        var response = await SendAsync(body, "deadbeef");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.BackgroundJobsMock.Verify(
            j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Post_InvalidJson_Returns400()
    {
        _factory.Reset();

        const string body = "not-json";

        var response = await SendAsync(body, Sign(body));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_TransactionsReady_EnqueuesSyncJob()
    {
        _factory.Reset();
        var account = MakeAccount("item_abc");
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByPlaidItemIdAsync("item_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        const string body = """{"webhook_type":"TRANSACTIONS","webhook_code":"TRANSACTIONS_READY","item_id":"item_abc"}""";

        var response = await SendAsync(body, Sign(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.BackgroundJobsMock.Verify(
            j => j.Create(
                It.Is<Job>(job =>
                    job.Type == typeof(ScheduledSyncJob) &&
                    job.Method.Name == nameof(ScheduledSyncJob.ExecuteSyncAsync) &&
                    (Guid)job.Args[0] == account.Id),
                It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task Post_SyncUpdatesAvailable_EnqueuesSyncJob()
    {
        _factory.Reset();
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByPlaidItemIdAsync("item_sync", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeAccount("item_sync"));

        const string body = """{"webhook_type":"SYNC_UPDATES_AVAILABLE","webhook_code":"HISTORICAL_UPDATE","item_id":"item_sync"}""";

        var response = await SendAsync(body, Sign(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.BackgroundJobsMock.Verify(
            j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task Post_ItemLoginRequired_MarksAccountForReauth()
    {
        _factory.Reset();
        var account = MakeAccount("item_reauth");
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByPlaidItemIdAsync("item_reauth", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        const string body = """{"webhook_type":"ITEM","webhook_code":"ERROR","item_id":"item_reauth","error":{"error_code":"ITEM_LOGIN_REQUIRED"}}""";

        var response = await SendAsync(body, Sign(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.BankAccountRepoMock.Verify(
            r => r.UpdateAsync(
                It.Is<BankAccount>(a => a.Id == account.Id && a.SyncStatus == "reauth_required"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _factory.BackgroundJobsMock.Verify(
            j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Post_UnknownWebhookType_ReturnsOkWithoutSideEffects()
    {
        _factory.Reset();

        const string body = """{"webhook_type":"SOMETHING_NEW","webhook_code":"X","item_id":"item_x"}""";

        var response = await SendAsync(body, Sign(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.BackgroundJobsMock.Verify(
            j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
        _factory.BankAccountRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Post_ItemIdWithNoMatchingAccount_DoesNotEnqueue()
    {
        _factory.Reset();
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByPlaidItemIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankAccount?)null);

        const string body = """{"webhook_type":"TRANSACTIONS","webhook_code":"TRANSACTIONS_READY","item_id":"item_missing"}""";

        var response = await SendAsync(body, Sign(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.BackgroundJobsMock.Verify(
            j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> SendAsync(string body, string signature)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = content,
        };
        request.Headers.Add("Plaid-Verification", signature);
        return await _client.SendAsync(request);
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    private static BankAccount MakeAccount(string externalAccountId)
    {
        return new BankAccount(
            userId: Guid.NewGuid(),
            externalAccountId: externalAccountId,
            bankName: "Test Bank",
            accountType: "depository",
            accountNumberLast4: "1234",
            ownerName: "Test Owner",
            currency: "USD",
            createdBy: Guid.NewGuid(),
            provider: "plaid");
    }
}

public class WebhookApiFactory : WebApplicationFactory<Program>
{
    public Mock<IBankAccountRepository> BankAccountRepoMock { get; } = new(MockBehavior.Loose);
    public Mock<IBackgroundJobClient> BackgroundJobsMock { get; } = new(MockBehavior.Loose);

    public void Reset()
    {
        BankAccountRepoMock.Reset();
        BackgroundJobsMock.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceService(services, BankAccountRepoMock.Object);
            ReplaceService(services, BackgroundJobsMock.Object);

            ReplaceDbContextWithInMemory<BankSyncDbContext>(services, "banksync-webhook-tests");
            ReplaceDbContextWithInMemory<FinanceSentry.Modules.Auth.Infrastructure.Persistence.AuthDbContext>(
                services, "auth-webhook-tests");

            services.Configure<FinanceSentry.Infrastructure.Encryption.EncryptionOptions>(opts =>
            {
                opts.CurrentKeyVersion = 1;
                opts.Keys = new Dictionary<int, string>
                {
                    [1] = "dGVzdGtleS10ZXN0a2V5LXRlc3RrZXktdGVzdGtleTA="
                };
            });
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
        builder.UseSetting("Plaid:WebhookKey", "test-webhook-key-for-integration");
        builder.UseSetting("Jwt:Secret",
            "test-jwt-secret-key-for-integration-tests-minimum-32-chars");
    }

    private static void ReplaceService<T>(IServiceCollection services, T implementation)
        where T : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors)
            services.Remove(d);
        services.AddScoped(_ => implementation);
    }

    private static void ReplaceDbContextWithInMemory<TContext>(IServiceCollection services, string dbName)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>)
                     || d.ServiceType == typeof(TContext)
                     || d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<TContext>))
            .ToList();
        foreach (var d in toRemove)
            services.Remove(d);

        services.AddDbContext<TContext>(options => options.UseInMemoryDatabase(dbName));
    }
}
