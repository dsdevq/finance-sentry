namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;
using IPlaidAdapterInterface = Modules.BankSync.Infrastructure.Plaid.IPlaidAdapter;

/// <summary>
/// Unit tests for ScheduledSyncService (T313).
/// All external dependencies are mocked; no database or network required.
/// </summary>
public class ScheduledSyncServiceTests
{
    // ── Shared test data ────────────────────────────────────────────────────
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static BankAccount MakeActiveAccount()
    {
        var account = new BankAccount(UserId, "item_abc", "Test Bank", "checking", "1234", "Jane", "EUR", UserId);
        account.BeginSync();
        account.MarkActive(1000m);
        return account;
    }

    private static EncryptedCredential MakeCredential(Guid accountId)
        => new(
            accountId,
            encryptedData: new byte[32],          // non-empty encrypted data
            iv: new byte[12],           // exactly 12 bytes
            authTag: new byte[16],           // exactly 16 bytes
            keyVersion: 1);

    // ── Mocks + SUT factory ─────────────────────────────────────────────────

    private (ScheduledSyncService sut,
             Mock<IBankAccountRepository> accountRepo,
             Mock<ITransactionRepository> txRepo,
             Mock<ISyncJobRepository> jobRepo,
             Mock<IEncryptedCredentialRepository> credRepo,
             Mock<ICredentialEncryptionService> encryption,
             Mock<IPlaidAdapterInterface> plaid,
             Mock<ITransactionDeduplicationService> dedup,
             Mock<IBankSyncLogger> logger) BuildSut()
    {
        var accountRepo = new Mock<IBankAccountRepository>();
        var txRepo = new Mock<ITransactionRepository>();
        var jobRepo = new Mock<ISyncJobRepository>();
        var credRepo = new Mock<IEncryptedCredentialRepository>();
        var encryption = new Mock<ICredentialEncryptionService>();
        var plaid = new Mock<IPlaidAdapterInterface>();
        var dedup = new Mock<ITransactionDeduplicationService>();
        var logger = new Mock<IBankSyncLogger>();

        var providerFactory = new Mock<IBankProviderFactory>();
        var monobankCreds = new Mock<IMonobankCredentialRepository>();
        var alertGen = new Mock<FinanceSentry.Core.Interfaces.IAlertGeneratorService>();
        var userPrefs = new Mock<FinanceSentry.Core.Interfaces.IUserAlertPreferencesReader>();

        var sut = new ScheduledSyncService(
            accountRepo.Object, txRepo.Object, jobRepo.Object, credRepo.Object,
            encryption.Object, plaid.Object, dedup.Object, logger.Object,
            providerFactory.Object, monobankCreds.Object,
            alertGen.Object, userPrefs.Object);

        return (sut, accountRepo, txRepo, jobRepo, credRepo, encryption, plaid, dedup, logger);
    }

    // ── T313-1: Account not found ───────────────────────────────────────────

    [Fact]
    public async Task PerformFullSyncAsync_AccountNotFound_ReturnsFailure()
    {
        var (sut, accountRepo, _, _, _, _, _, _, _) = BuildSut();

        accountRepo.Setup(r => r.GetByIdAsync(AccountId, default))
                   .ReturnsAsync((BankAccount?)null);

        var result = await sut.PerformFullSyncAsync(AccountId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT_NOT_FOUND");
    }

    // ── T313-2: Successful sync ─────────────────────────────────────────────

    [Fact]
    public async Task PerformFullSyncAsync_HappyPath_CreatesJobFetchesAndSavesTransactions()
    {
        var (sut, accountRepo, txRepo, jobRepo, credRepo, encryption, plaid, dedup, _) = BuildSut();

        var account = MakeActiveAccount();
        var credential = MakeCredential(AccountId);

        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(account);
        accountRepo.Setup(r => r.UpdateAsync(It.IsAny<BankAccount>(), default)).ReturnsAsync(account);

        jobRepo.Setup(r => r.AddAsync(It.IsAny<SyncJob>(), default))
               .ReturnsAsync((SyncJob j, CancellationToken _) => j);
        jobRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncJob>(), default))
               .ReturnsAsync((SyncJob j, CancellationToken _) => j);
        jobRepo.Setup(r => r.GetLatestByAccountIdAsync(It.IsAny<Guid>(), default))
               .ReturnsAsync((SyncJob?)null);

        credRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(credential);
        encryption.Setup(e => e.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>()))
                  .Returns("access-sandbox-token");

        var candidates = new List<TransactionCandidate>
        {
            new(AccountId, UserId, 50m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1),
                "Coffee", false, "debit", "Starbucks", "food", "tx_001"),
            new(AccountId, UserId, 100m, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2),
                "Salary", false, "credit", null, "income", "tx_002")
        };

        plaid.Setup(p => p.SyncTransactionsAsync(
                "access-sandbox-token", It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), default))
             .ReturnsAsync(((IReadOnlyList<TransactionCandidate>)candidates, "cursor_1"));

        plaid.Setup(p => p.GetAccountsWithBalanceAsync("access-sandbox-token", default))
             .ReturnsAsync(
             [
                 new("plaid_acc_1", "Checking", "checking", "1234", 1200m, 1200m, "EUR")
             ]);

        txRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default))
              .ReturnsAsync([]);

        dedup.Setup(d => d.FilterDuplicates(
                It.IsAny<IEnumerable<TransactionCandidate>>(),
                It.IsAny<IReadOnlySet<string>>()))
             .Returns(candidates);

        var entity1 = new Transaction(AccountId, UserId, 50m, DateTime.UtcNow.AddDays(-1), "Coffee", "hash1", false);
        var entity2 = new Transaction(AccountId, UserId, 100m, DateTime.UtcNow.AddDays(-2), "Salary", "hash2", false);

        dedup.Setup(d => d.ToEntity(candidates[0])).Returns(entity1);
        dedup.Setup(d => d.ToEntity(candidates[1])).Returns(entity2);

        txRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), default))
              .ReturnsAsync((IEnumerable<Transaction> txs, CancellationToken _) => txs);

        var result = await sut.PerformFullSyncAsync(AccountId);

        result.Success.Should().BeTrue();
        result.TransactionCountFetched.Should().Be(2);
        result.TransactionCountDeduped.Should().Be(2);

        jobRepo.Verify(r => r.AddAsync(It.IsAny<SyncJob>(), default), Times.Once);
        txRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), default), Times.Once);
        accountRepo.Verify(r => r.UpdateAsync(It.IsAny<BankAccount>(), default), Times.AtLeast(2));
    }

    // ── T313-3: Deduplication filters existing transactions ─────────────────

    [Fact]
    public async Task PerformFullSyncAsync_DuplicatesFiltered_SavesOnlyNewTransactions()
    {
        var (sut, accountRepo, txRepo, jobRepo, credRepo, encryption, plaid, dedup, _) = BuildSut();

        var account = MakeActiveAccount();
        var credential = MakeCredential(AccountId);

        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(account);
        accountRepo.Setup(r => r.UpdateAsync(It.IsAny<BankAccount>(), default)).ReturnsAsync(account);
        jobRepo.Setup(r => r.AddAsync(It.IsAny<SyncJob>(), default))
               .ReturnsAsync((SyncJob j, CancellationToken _) => j);
        jobRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncJob>(), default))
               .ReturnsAsync((SyncJob j, CancellationToken _) => j);
        jobRepo.Setup(r => r.GetLatestByAccountIdAsync(It.IsAny<Guid>(), default))
               .ReturnsAsync((SyncJob?)null);
        credRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(credential);
        encryption.Setup(e => e.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>()))
                  .Returns("access-sandbox-token");

        var allCandidates = new List<TransactionCandidate>
        {
            new(AccountId, UserId, 50m, DateTime.UtcNow.AddDays(-1), null, "Coffee", true, "debit", null, null, "tx_001"),
            new(AccountId, UserId, 100m, DateTime.UtcNow.AddDays(-2), null, "Salary", true, "credit", null, null, "tx_002")
        };

        plaid.Setup(p => p.SyncTransactionsAsync(
                "access-sandbox-token", It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), default))
             .ReturnsAsync(((IReadOnlyList<TransactionCandidate>)allCandidates, "cursor_1"));

        plaid.Setup(p => p.GetAccountsWithBalanceAsync("access-sandbox-token", default))
             .ReturnsAsync(
             [
                 new("plaid_acc_1", "Checking", "checking", "1234", 900m, 900m, "EUR")
             ]);

        // Only one existing transaction in DB
        var existingTx = new Transaction(AccountId, UserId, 50m, DateTime.UtcNow.AddDays(-1), "Coffee", "hash_existing", false);
        txRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default))
              .ReturnsAsync([existingTx]);

        // Dedup returns only the NEW candidate (the existing one is filtered out)
        var newCandidates = allCandidates.Skip(1).ToList();
        dedup.Setup(d => d.FilterDuplicates(
                It.IsAny<IEnumerable<TransactionCandidate>>(),
                It.IsAny<IReadOnlySet<string>>()))
             .Returns(newCandidates);

        var newEntity = new Transaction(AccountId, UserId, 100m, DateTime.UtcNow.AddDays(-2), "Salary", "hash_new", false);
        dedup.Setup(d => d.ToEntity(newCandidates[0])).Returns(newEntity);

        txRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), default))
              .ReturnsAsync((IEnumerable<Transaction> txs, CancellationToken _) => txs);

        var result = await sut.PerformFullSyncAsync(AccountId);

        result.Success.Should().BeTrue();
        result.TransactionCountFetched.Should().Be(2);  // 2 from Plaid
        result.TransactionCountDeduped.Should().Be(1);  // 1 new after dedup
    }

    // ── T313-4: Exception during sync marks job failed ──────────────────────

    [Fact]
    public async Task PerformFullSyncAsync_PlaidThrows_MarksJobAndAccountFailed()
    {
        var (sut, accountRepo, txRepo, jobRepo, credRepo, encryption, plaid, dedup, logger) = BuildSut();

        var account = MakeActiveAccount();
        var credential = MakeCredential(AccountId);

        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(account);
        accountRepo.Setup(r => r.UpdateAsync(It.IsAny<BankAccount>(), default)).ReturnsAsync(account);
        jobRepo.Setup(r => r.AddAsync(It.IsAny<SyncJob>(), default))
               .ReturnsAsync((SyncJob j, CancellationToken _) => j);
        jobRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncJob>(), default))
               .ReturnsAsync((SyncJob j, CancellationToken _) => j);
        jobRepo.Setup(r => r.GetLatestByAccountIdAsync(It.IsAny<Guid>(), default))
               .ReturnsAsync((SyncJob?)null);
        credRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(credential);
        encryption.Setup(e => e.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>()))
                  .Returns("access-sandbox-token");
        txRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default))
              .ReturnsAsync([]);

        plaid.Setup(p => p.SyncTransactionsAsync(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), default))
             .ThrowsAsync(new HttpRequestException("RATE_LIMIT_EXCEEDED: too many requests"));

        var result = await sut.PerformFullSyncAsync(AccountId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("RATE_LIMIT_EXCEEDED");
        jobRepo.Verify(r => r.UpdateAsync(It.Is<SyncJob>(j => j.Status == "failed"), default), Times.Once);
    }

    // ── T313-5: Idempotency — coordinator blocks concurrent runs ────────────

    [Fact]
    public async Task TriggerScheduledSyncAsync_AlreadyRunning_ReturnsEarlyWithoutNewSync()
    {
        var syncJobRepo = new Mock<ISyncJobRepository>();
        var syncService = new Mock<IScheduledSyncService>();

        syncJobRepo.Setup(r => r.HasRunningJobAsync(AccountId, default)).ReturnsAsync(true);

        var coordinator = new TransactionSyncCoordinator(syncJobRepo.Object, syncService.Object);

        var result = await coordinator.TriggerScheduledSyncAsync(AccountId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("SYNC_IN_PROGRESS");
        syncService.Verify(s => s.PerformFullSyncAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
