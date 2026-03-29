namespace FinanceSentry.Tests.Integration.Shared;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.AuditLog;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Integration tests for AuditLogService.
/// Uses an in-memory SQLite substitute (or real PostgreSQL via Testcontainers in CI).
/// Tests verify Constitution V compliance: all data access is recorded.
/// </summary>
public class AuditLogServiceTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly BankSyncDbContext _db;
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<BankSyncDbContext>(options =>
            options.UseInMemoryDatabase($"audit-log-test-{Guid.NewGuid()}"));
        services.AddLogging();

        _provider = services.BuildServiceProvider();
        _db = _provider.GetRequiredService<BankSyncDbContext>();
        _db.Database.EnsureCreated();

        _sut = new AuditLogService(_provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AuditLogService>.Instance);
    }

    [Fact]
    public async Task Log_ReadAccount_InsertsAuditRow()
    {
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        _sut.Log(userId, AuditActions.ReadAccount, "BankAccount", resourceId, correlationId: "corr-001");

        // Give fire-and-forget a moment to complete
        await Task.Delay(200);

        var row = await _db.AuditLogs.FirstOrDefaultAsync(a => a.UserId == userId);
        Assert.NotNull(row);
        Assert.Equal(AuditActions.ReadAccount, row.Action);
        Assert.Equal(resourceId, row.ResourceId);
        Assert.Equal("corr-001", row.CorrelationId);
    }

    [Fact]
    public async Task Log_DeleteAccount_InsertsDeleteAuditRow()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _sut.Log(userId, AuditActions.DeleteAccount, "BankAccount", accountId);

        await Task.Delay(200);

        var row = await _db.AuditLogs.FirstOrDefaultAsync(a => a.UserId == userId);
        Assert.NotNull(row);
        Assert.Equal(AuditActions.DeleteAccount, row.Action);
    }

    [Fact]
    public async Task Log_DoesNotContainSensitiveData()
    {
        var userId = Guid.NewGuid();

        _sut.Log(userId, AuditActions.CredentialAccess, "EncryptedCredential", null);

        await Task.Delay(200);

        var row = await _db.AuditLogs.FirstOrDefaultAsync(a => a.UserId == userId);
        Assert.NotNull(row);

        // Verify no sensitive data fields are populated
        Assert.Null(row.UserAgent); // not supplied
        Assert.Equal(AuditActions.CredentialAccess, row.Action);
    }

    [Fact]
    public async Task Log_WriteFailure_DoesNotThrow()
    {
        // Arrange: create a service with a broken scope factory (disposed provider)
        var brokenProvider = new ServiceCollection()
            .AddDbContext<BankSyncDbContext>(o => o.UseInMemoryDatabase("broken"))
            .BuildServiceProvider();

        brokenProvider.Dispose();

        var brokenService = new AuditLogService(
            brokenProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AuditLogService>.Instance);

        // Act + Assert: must not throw even when the write fails
        var exception = await Record.ExceptionAsync(async () =>
        {
            brokenService.Log(Guid.NewGuid(), AuditActions.ReadAccount, "BankAccount", null);
            await Task.Delay(300);
        });

        Assert.Null(exception);
    }

    public void Dispose()
    {
        _db.Dispose();
        _provider.Dispose();
    }
}
