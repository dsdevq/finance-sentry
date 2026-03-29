namespace FinanceSentry.Tests.Unit.BankSync.API;

using FinanceSentry.Modules.BankSync.Infrastructure.Security;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for webhook security and routing (T314).
/// </summary>
public class WebhookHandlerTests
{
    // ── WebhookSignatureValidator ────────────────────────────────────────────

    private readonly IWebhookSignatureValidator _validator = new WebhookSignatureValidator();

    [Fact]
    public void IsValid_CorrectHmac_ReturnsTrue()
    {
        const string webhookKey = "my-super-secret-webhook-key";
        const string rawBody    = """{"webhook_type":"TRANSACTIONS","webhook_code":"TRANSACTIONS_READY","item_id":"item_123"}""";

        // Compute the expected HMAC manually
        var keyBytes  = System.Text.Encoding.UTF8.GetBytes(webhookKey);
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(rawBody);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var expectedHex = Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLowerInvariant();

        _validator.IsValid(rawBody, expectedHex, webhookKey).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WrongSignature_ReturnsFalse()
    {
        const string webhookKey    = "my-super-secret-webhook-key";
        const string rawBody       = """{"webhook_type":"TRANSACTIONS","item_id":"item_123"}""";
        const string badSignature  = "deadbeef00000000000000000000000000000000000000000000000000000000";

        _validator.IsValid(rawBody, badSignature, webhookKey).Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptyBody_ReturnsFalse()
    {
        _validator.IsValid(string.Empty, "somesig", "somekey").Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptySignature_ReturnsFalse()
    {
        _validator.IsValid("body", string.Empty, "somekey").Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptyKey_ReturnsFalse()
    {
        _validator.IsValid("body", "somesig", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsValid_TamperedBody_ReturnsFalse()
    {
        const string webhookKey    = "my-super-secret-webhook-key";
        const string originalBody  = """{"webhook_type":"TRANSACTIONS","item_id":"item_123"}""";
        const string tamperedBody  = """{"webhook_type":"TRANSACTIONS","item_id":"item_HACKED"}""";

        var keyBytes  = System.Text.Encoding.UTF8.GetBytes(webhookKey);
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(originalBody);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var originalSig = Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLowerInvariant();

        // Use original signature but tampered body — should fail
        _validator.IsValid(tamperedBody, originalSig, webhookKey).Should().BeFalse();
    }

    // ── Routing logic via TransactionSyncCoordinator ─────────────────────────
    // The WebhookController itself requires ASP.NET Core hosting context; routing
    // logic is delegated to TransactionSyncCoordinator which has its own tests.
    // The tests below verify that coordinator correctly drops concurrent syncs.

    [Fact]
    public async Task TransactionSyncCoordinator_WebhookSync_AlreadyRunning_DropsRequest()
    {
        var syncJobRepo = new Moq.Mock<FinanceSentry.Modules.BankSync.Domain.Repositories.ISyncJobRepository>();
        var syncService = new Moq.Mock<FinanceSentry.Modules.BankSync.Application.Services.IScheduledSyncService>();

        var accountId = Guid.NewGuid();
        syncJobRepo.Setup(r => r.HasRunningJobAsync(accountId, default)).ReturnsAsync(true);

        var coordinator = new FinanceSentry.Modules.BankSync.Application.Services.TransactionSyncCoordinator(
            syncJobRepo.Object, syncService.Object);

        var result = await coordinator.TriggerWebhookSyncAsync(accountId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("SYNC_IN_PROGRESS");
        syncService.Verify(
            s => s.PerformFullSyncAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<System.Threading.CancellationToken>()),
            Moq.Times.Never);
    }

    [Fact]
    public async Task TransactionSyncCoordinator_WebhookSync_NoRunningJob_ForwardsTriggerAsWebhook()
    {
        var syncJobRepo = new Moq.Mock<FinanceSentry.Modules.BankSync.Domain.Repositories.ISyncJobRepository>();
        var syncService = new Moq.Mock<FinanceSentry.Modules.BankSync.Application.Services.IScheduledSyncService>();

        var accountId = Guid.NewGuid();
        syncJobRepo.Setup(r => r.HasRunningJobAsync(accountId, default)).ReturnsAsync(false);
        syncService.Setup(s => s.PerformFullSyncAsync(accountId, true, default))
                   .ReturnsAsync(new FinanceSentry.Modules.BankSync.Application.Services.SyncResult(true, 5, 3, null, null));

        var coordinator = new FinanceSentry.Modules.BankSync.Application.Services.TransactionSyncCoordinator(
            syncJobRepo.Object, syncService.Object);

        var result = await coordinator.TriggerWebhookSyncAsync(accountId);

        result.Success.Should().BeTrue();
        syncService.Verify(s => s.PerformFullSyncAsync(accountId, true, default), Moq.Times.Once);
    }

    [Fact]
    public async Task TransactionSyncCoordinator_ManualSync_NoRunningJob_DelegatesToService()
    {
        var syncJobRepo = new Moq.Mock<FinanceSentry.Modules.BankSync.Domain.Repositories.ISyncJobRepository>();
        var syncService = new Moq.Mock<FinanceSentry.Modules.BankSync.Application.Services.IScheduledSyncService>();

        var accountId = Guid.NewGuid();
        syncJobRepo.Setup(r => r.HasRunningJobAsync(accountId, default)).ReturnsAsync(false);
        syncService.Setup(s => s.PerformFullSyncAsync(accountId, false, default))
                   .ReturnsAsync(new FinanceSentry.Modules.BankSync.Application.Services.SyncResult(true, 10, 10, null, null));

        var coordinator = new FinanceSentry.Modules.BankSync.Application.Services.TransactionSyncCoordinator(
            syncJobRepo.Object, syncService.Object);

        var result = await coordinator.TriggerManualSyncAsync(accountId);

        result.Success.Should().BeTrue();
    }
}
