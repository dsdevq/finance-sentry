namespace FinanceSentry.Tests.Integration.BankSync;

using System.Net;
using System.Net.Http.Json;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

/// <summary>
/// REST API contract tests (T215).
/// Validates response shapes for all US1 endpoints:
///   POST /accounts/connect
///   POST /accounts/link
///   GET  /accounts
///   GET  /accounts/{id}/transactions
///
/// Uses WebApplicationFactory with mocked infrastructure dependencies
/// (no real Plaid calls, no real database).
/// </summary>
public class BankSyncAPIContractTests : IClassFixture<BankSyncApiFactory>
{
    private readonly HttpClient _client;
    private readonly BankSyncApiFactory _factory;

    public BankSyncAPIContractTests(BankSyncApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /accounts/connect ───────────────────────────────────────────────

    [Fact]
    public async Task PostConnect_Returns200_WithLinkTokenShape()
    {
        _factory.PlaidClientMock
            .Setup(c => c.CreateLinkTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidLinkTokenResponse(
                "link-sandbox-test-token",
                "req_001",
                DateTime.UtcNow.AddMinutes(30)));

        var response = await _client.PostAsync("/api/accounts/connect",
            JsonContent.Create(new { userId = Guid.NewGuid() }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ConnectResponse>();
        body!.LinkToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresIn.Should().BeGreaterThan(0);
    }

    // ── GET /accounts ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccounts_Returns200_WithAccountListShape()
    {
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await _client.GetAsync("/api/accounts?userId=" + Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountsListResponse>();
        body.Should().NotBeNull();
        body!.Accounts.Should().NotBeNull();
        body.TotalCount.Should().Be(0);
        body.CurrencyTotals.Should().NotBeNull();
    }

    // ── GET /accounts/{id}/transactions ─────────────────────────────────────

    [Fact]
    public async Task GetTransactions_Returns404_WhenAccountNotOwnedByUser()
    {
        var accountId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        // Repository returns account owned by a DIFFERENT user
        _factory.BankAccountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceSentry.Modules.BankSync.Domain.BankAccount(
                userId: Guid.NewGuid(), // different user
                plaidItemId: "item_xxx",
                bankName: "AIB",
                accountType: "checking",
                accountNumberLast4: "1234",
                ownerName: "Other Person",
                currency: "EUR",
                createdBy: Guid.NewGuid()));

        var url = $"/api/accounts/{accountId}/transactions?userId={requestingUserId}";
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "FR-009: users must not access other users' account data");
    }

    [Fact]
    public async Task GetTransactions_Returns200_WithPaginatedShape()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _factory.BankAccountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceSentry.Modules.BankSync.Domain.BankAccount(
                userId: userId, // same user as requesting
                plaidItemId: "item_abc",
                bankName: "Revolut",
                accountType: "checking",
                accountNumberLast4: "5678",
                ownerName: "Jane Doe",
                currency: "EUR",
                createdBy: userId));

        _factory.TransactionRepoMock
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FinanceSentry.Modules.BankSync.Domain.Transaction>());
        _factory.TransactionRepoMock
            .Setup(r => r.CountByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var url = $"/api/accounts/{accountId}/transactions?userId={userId}&offset=0&limit=50";
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionsListResponse>();
        body.Should().NotBeNull();
        body!.Transactions.Should().NotBeNull();
        body.TotalCount.Should().Be(0);
        body.HasMore.Should().BeFalse();
    }

    // ── Response shape records (contract definitions) ────────────────────────

    private record ConnectResponse(string LinkToken, int ExpiresIn, string RequestId);
    private record AccountsListResponse(object[] Accounts, int TotalCount, object CurrencyTotals);
    private record TransactionsListResponse(object[] Transactions, int TotalCount, bool HasMore);
}

/// <summary>
/// Custom WebApplicationFactory that replaces infrastructure with mocks.
/// </summary>
public class BankSyncApiFactory : WebApplicationFactory<Program>
{
    public Mock<IPlaidClient> PlaidClientMock { get; } = new(MockBehavior.Loose);
    public Mock<IBankAccountRepository> BankAccountRepoMock { get; } = new(MockBehavior.Loose);
    public Mock<ITransactionRepository> TransactionRepoMock { get; } = new(MockBehavior.Loose);

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override real infrastructure with mocks
            ReplaceService(services, PlaidClientMock.Object);
            ReplaceService(services, BankAccountRepoMock.Object);
            ReplaceService(services, TransactionRepoMock.Object);

            // Provide minimal configuration to satisfy Program.cs startup
            services.Configure<FinanceSentry.Infrastructure.Encryption.EncryptionOptions>(opts =>
            {
                opts.CurrentKeyVersion = 1;
                opts.Keys = new Dictionary<int, string>
                {
                    [1] = "dGVzdGtleS10ZXN0a2V5LXRlc3RrZXktdGVzdGtleTA=" // test key
                };
            });
        });

        builder.UseEnvironment("Testing");

        // Inject required config values so Program.cs startup checks don't throw
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
    }

    private static void ReplaceService<T>(IServiceCollection services, T implementation)
        where T : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
        services.AddScoped<T>(_ => implementation);
    }
}
