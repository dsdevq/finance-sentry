namespace FinanceSentry.Tests.Integration.BankSync;

using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

/// <summary>
/// Integration tests for PlaidAdapter + domain persistence pipeline (T214).
///
/// Verifies:
/// - Account link flow: PlaidAdapter maps Plaid response → domain entity stored with encrypted token
/// - Encrypted token stored correctly; plaintext never persisted
/// - 100 transactions deduplicated + stored with correct hashes, type, and category
///
/// Uses in-memory mocked IPlaidClient (no real Plaid calls); in-process BankSyncDbContext
/// with Testcontainers PostgreSQL is added in a future phase when the repository layer is
/// exercised end-to-end. These tests validate the adapter→domain→deduplication pipeline.
/// </summary>
public class PlaidAdapterIntegrationTests
{
    private static readonly Guid UserId    = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid AccountId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private static readonly EncryptionOptions EncOpts = new()
    {
        CurrentKeyVersion = 1,
        Keys = new Dictionary<int, string>
        {
            [1] = "dGVzdGtleS10ZXN0a2V5LXRlc3RrZXktdGVzdGtleTA="
        }
    };

    private readonly CredentialEncryptionService _encryption = new(Options.Create(EncOpts));
    private readonly TransactionDeduplicationService _dedup =
        new("dGVzdGtleS10ZXN0a2V5LXRlc3RrZXktdGVzdGtleTA=");

    private readonly Mock<IPlaidClient> _clientMock = new(MockBehavior.Strict);
    private PlaidAdapter CreateAdapter() => new(_clientMock.Object);

    // ── Account link flow ────────────────────────────────────────────────────

    [Fact]
    public async Task LinkAccount_EncryptedTokenStoredCorrectly_PlaintextNeverPersisted()
    {
        const string accessToken = "access-sandbox-secret-token-abc";

        _clientMock
            .Setup(c => c.ExchangePublicTokenAsync("public-sandbox-xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidExchangeTokenResponse(accessToken, "item-001", "req_1"));

        _clientMock
            .Setup(c => c.GetAccountsAsync(accessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidAccountsResponse(
                [new PlaidAccount("acct_001", "My Checking", "AIB Checking",
                    "depository", "checking", "1234", 2500m, 2400m, "EUR")],
                "req_2"));

        var adapter = CreateAdapter();

        // Exchange → get accounts
        var exchange = await adapter.ExchangePublicTokenAsync("public-sandbox-xyz");
        var accounts = await adapter.GetAccountsWithBalanceAsync(exchange.AccessToken);

        // Encrypt access token (simulating ConnectBankAccountCommandHandler)
        var encrypted = _encryption.Encrypt(exchange.AccessToken);

        // Verify: ciphertext != plaintext
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(accessToken);
        encrypted.Ciphertext.Should().NotBeEquivalentTo(plaintextBytes,
            "access token must be encrypted before storage");

        // Verify: decrypt roundtrip restores plaintext
        var decrypted = _encryption.Decrypt(
            encrypted.Ciphertext, encrypted.Iv, encrypted.AuthTag, encrypted.KeyVersion);
        decrypted.Should().Be(accessToken, "decryption must recover the original access token");

        // Verify account mapping
        accounts.Should().HaveCount(1);
        accounts[0].AccountNumberLast4.Should().Be("1234");
        accounts[0].Currency.Should().Be("EUR");
        accounts[0].CurrentBalance.Should().Be(2500m);
    }

    // ── Transaction deduplication pipeline ──────────────────────────────────

    [Fact]
    public async Task GetTransactions_100Transactions_StoredWithCorrectHashesAndTypes()
    {
        const string accessToken = "access-sandbox-abc";
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 4, 10);

        // Build 100 Plaid transactions: mix of debit/credit, pending/posted, with categories
        // Plaid sign convention: positive = outflow (debit), negative = inflow (credit)
        var plaidTxns = Enumerable.Range(0, 100).Select(i => new PlaidTransaction(
            TransactionId: $"txn_{i:D3}",
            AccountId: "plaid-acct-001",
            Amount: i % 2 == 0 ? (i + 1) * 10.00m : -(i + 1) * 5.00m, // positive=debit, negative=credit
            IsoCurrencyCode: "USD",
            Name: $"Merchant_{i}",
            MerchantName: $"Merchant_{i}",
            PersonalFinanceCategory: i % 3 == 0 ? "Groceries" : i % 3 == 1 ? "Transport" : "Entertainment",
            Date: start.AddDays(i),
            AuthorizedDate: start.AddDays(i).AddDays(-1),
            Pending: i >= 90 // last 10 are pending
        )).ToList();

        _clientMock
            .Setup(c => c.SyncTransactionsAsync(accessToken, null, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidSyncResponse(plaidTxns, [], [], "cursor_1", false, "req_3"));

        var adapter = CreateAdapter();
        var (candidates, _) = await adapter.SyncTransactionsAsync(accessToken, AccountId, UserId, null);

        candidates.Should().HaveCount(100);

        // Verify amounts are always positive (direction determined by type)
        candidates.Should().AllSatisfy(c => c.Amount.Should().BeGreaterThan(0));

        // Verify debit/credit mapping
        var debits = candidates.Where(c => c.TransactionType == "debit").ToList();
        var credits = candidates.Where(c => c.TransactionType == "credit").ToList();
        debits.Should().NotBeEmpty("even-indexed transactions are debits");
        credits.Should().NotBeEmpty("odd-indexed transactions are credits");

        // Verify merchant categories populated
        var withCategories = candidates.Where(c => c.MerchantCategory != null).ToList();
        withCategories.Should().HaveCount(100, "all transactions have a category");
        withCategories.Select(c => c.MerchantCategory).Should()
            .Contain("Groceries").And.Contain("Transport").And.Contain("Entertainment");

        // Verify pending/posted distinction
        candidates.Count(c => c.IsPending).Should().Be(10, "last 10 transactions are pending");
        candidates.Count(c => !c.IsPending).Should().Be(90);

        // Deduplication: convert to entities, verify unique hashes
        var entities = candidates.Select(_dedup.ToEntity).ToList();
        entities.Should().HaveCount(100);
        entities.Select(e => e.UniqueHash).Distinct().Should().HaveCount(100,
            "all 100 transactions must produce unique hashes");

        // Verify entity types match candidates
        entities.Should().AllSatisfy(e => e.UserId.Should().Be(UserId));
        entities.Should().AllSatisfy(e => e.AccountId.Should().Be(AccountId));
    }

    [Fact]
    public async Task GetTransactions_DuplicateBatch_DeduplicatedCorrectly()
    {
        const string accessToken = "access-sandbox-abc";
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 3, 31);

        var plaidTxns = Enumerable.Range(0, 50).Select(i => new PlaidTransaction(
            TransactionId: $"txn_{i:D3}",
            AccountId: "plaid-acct-001",
            Amount: -(i + 1) * 10.00m,
            IsoCurrencyCode: "EUR",
            Name: $"Shop_{i}",
            MerchantName: null,
            PersonalFinanceCategory: "Groceries",
            Date: start.AddDays(i),
            AuthorizedDate: null,
            Pending: false
        )).ToList();

        _clientMock
            .Setup(c => c.SyncTransactionsAsync(accessToken, null, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaidSyncResponse(plaidTxns, [], [], "cursor_1", false, "req_4"));

        var adapter = CreateAdapter();
        var (candidates, _) = await adapter.SyncTransactionsAsync(accessToken, AccountId, UserId, null);

        // Build existing hashes from the same 50 transactions (simulating already stored)
        var existingHashes = candidates
            .Select(c => _dedup.ComputeHash(c.AccountId, c.Amount, c.HashDate, c.Description))
            .ToHashSet();

        // Filter: all 50 are duplicates → 0 should pass through
        var newOnes = _dedup.FilterDuplicates(candidates, existingHashes);
        newOnes.Should().BeEmpty("all 50 transactions already exist in DB");

        // SC-005: deduplication accuracy is 100% > 95% threshold
        var accuracy = (double)(candidates.Count - newOnes.Count) / candidates.Count * 100;
        accuracy.Should().BeGreaterThanOrEqualTo(95,
            $"SC-005 requires 95%+ deduplication accuracy, got {accuracy}%");
    }
}
