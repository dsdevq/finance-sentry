namespace FinanceSentry.Tests.Unit.BankSync;

using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank;
using FinanceSentry.Modules.BankSync.Infrastructure.Monobank.History;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class MonobankHistorySourceTests
{
    private readonly Mock<IMonobankCredentialRepository> _credentialRepo = new(MockBehavior.Strict);
    private readonly Mock<IBankAccountRepository> _bankAccountRepo = new(MockBehavior.Strict);
    private readonly Mock<MonobankHttpClient> _httpClient = new(new HttpClient());
    private readonly Mock<ICredentialEncryptionService> _encryption = new(MockBehavior.Strict);

    private MonobankHistorySource CreateSource() =>
        new(_credentialRepo.Object, _bankAccountRepo.Object, _httpClient.Object,
            _encryption.Object, NullLogger<MonobankHistorySource>.Instance);

    [Fact]
    public async Task GetMonthlyBalancesAsync_NoCredential_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();
        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonobankCredential?)null);

        var result = await CreateSource().GetMonthlyBalancesAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlyBalancesAsync_NoMonobankAccounts_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();
        var credential = new MonobankCredential(userId, [], [], []);

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _encryption
            .Setup(e => e.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns("test-token");
        _bankAccountRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccount>());

        var result = await CreateSource().GetMonthlyBalancesAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlyBalancesAsync_WithStatements_GroupsByMonthEnd()
    {
        var userId = Guid.NewGuid();
        var credential = new MonobankCredential(userId, [], [], []);
        var account = new BankAccount(userId, "acc1", "Monobank", "black", "0000", "John", "UAH", userId, "monobank");

        var marchTime = new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var marchEndTime = new DateTimeOffset(2024, 3, 28, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        var transactions = new List<MonobankTransaction>
        {
            new("t1", marchTime, "desc", 0, false, -100_00, 980, -100_00, 980, 0, 0, 1000_00, null, null, null, null, null),
            new("t2", marchEndTime, "desc", 0, false, -50_00, 980, -50_00, 980, 0, 0, 950_00, null, null, null, null, null),
        };

        _credentialRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _encryption
            .Setup(e => e.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns("test-token");
        _bankAccountRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccount> { account });
        _httpClient
            .Setup(c => c.GetStatementsAsync("test-token", "acc1", It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await CreateSource().GetMonthlyBalancesAsync(userId);

        result.Should().NotBeEmpty();
        result.All(r => r.AssetCategory == "banking").Should().BeTrue();
        var marchSnapshot = result.FirstOrDefault(r => r.MonthEnd.Month == 3 && r.MonthEnd.Year == 2024);
        marchSnapshot.Should().NotBeNull();
        marchSnapshot!.MonthEnd.Day.Should().Be(31);
    }
}
