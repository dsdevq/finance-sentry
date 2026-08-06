namespace FinanceSentry.Modules.BankSync.API.Controllers;

using System.Text.Json;
using FinanceSentry.Core.Api;
using FinanceSentry.Infrastructure.Logging;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Security;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Receives and routes Plaid webhook notifications.
/// All incoming payloads are signature-validated before processing (T306).
/// </summary>
[ApiController]
[Route("webhook")]
public class WebhookController(
    IWebhookSignatureValidator signatureValidator,
    IBackgroundJobClient backgroundJobs,
    IBankAccountRepository accounts,
    ITransactionSyncCoordinator coordinator,
    IBankSyncLogger logger,
    IConfiguration config) : ControllerBase
{
    private readonly IWebhookSignatureValidator _signatureValidator = signatureValidator;
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionSyncCoordinator _coordinator = coordinator;
    private readonly IBankSyncLogger _logger = logger;
    private readonly IConfiguration _config = config;

    // ── POST /api/webhook/plaid ── T305 ──────────────────────────────────────

    /// <summary>
    /// Receives Plaid webhook events.
    /// Validates HMAC-SHA256 signature, then routes by webhook_type.
    /// </summary>
    [HttpPost("plaid")]
    public async Task<IActionResult> HandlePlaidWebhook(CancellationToken ct)
    {
        // Read raw body for signature validation. EnableBuffering makes the request
        // stream seekable so the rewind below works under both Kestrel and TestHost.
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        // Validate signature
        var signatureHeader = Request.Headers["Plaid-Verification"].FirstOrDefault() ?? string.Empty;
        var webhookKey = _config["Plaid:WebhookKey"] ?? string.Empty;

        if (!_signatureValidator.IsValid(rawBody, signatureHeader, webhookKey))
            return Unauthorized(new ApiErrorBody("Invalid webhook signature.", "INVALID_SIGNATURE"));

        // Parse payload
        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(rawBody);
        }
        catch
        {
            return BadRequest(new ApiErrorBody("Invalid JSON payload.", "INVALID_PAYLOAD"));
        }

        var webhookType = root.TryGetProperty("webhook_type", out var wt) ? wt.GetString() ?? "" : "";
        var webhookCode = root.TryGetProperty("webhook_code", out var wc) ? wc.GetString() ?? "" : "";
        var correlationId = HttpContext.TraceIdentifier;

        _logger.WebhookReceived(correlationId, webhookType, webhookCode);

        // Route by webhook_type
        switch (webhookType.ToUpperInvariant())
        {
            case "TRANSACTIONS":
                if (webhookCode is "TRANSACTIONS_REMOVED" or "DEFAULT_UPDATE" or "INITIAL_UPDATE")
                    await EnqueueSyncFromWebhook(root, ct);
                else if (webhookCode == "TRANSACTIONS_READY")
                    await EnqueueSyncFromWebhook(root, ct);
                break;

            case "SYNC_UPDATES_AVAILABLE":
                await EnqueueSyncFromWebhook(root, ct);
                break;

            case "ITEM":
                await HandleItemWebhook(root, webhookCode, ct);
                break;

            default:
                // Unknown webhook_type — log and ignore
                break;
        }

        return Ok();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task EnqueueSyncFromWebhook(JsonElement root, CancellationToken ct)
    {
        var accountId = await ResolveAccountId(root, ct);
        if (accountId == null)
            return;

        _backgroundJobs.Enqueue<Infrastructure.Jobs.ScheduledSyncJob>(
            job => job.ExecuteSyncAsync(accountId.Value));
    }

    private async Task HandleItemWebhook(JsonElement root, string webhookCode, CancellationToken ct)
    {
        var errorCode = root.TryGetProperty("error", out var errEl)
            && errEl.TryGetProperty("error_code", out var ec)
            ? ec.GetString()
            : null;

        if (webhookCode == "ERROR" && errorCode == "ITEM_LOGIN_REQUIRED")
        {
            var accountId = await ResolveAccountId(root, ct);
            if (accountId == null)
                return;

            var account = await _accounts.GetByIdAsync(accountId.Value, ct);
            if (account != null)
            {
                account.MarkReauthRequired();
                await _accounts.UpdateAsync(account, ct);
            }
        }
        else if (webhookCode == "ERROR" && errorCode == "PRODUCT_NOT_READY")
        {
            // Log and ignore — will be retried by Plaid or the scheduler
        }
        // Other ITEM error codes: log, no action
    }

    /// <summary>
    /// Resolves the internal account ID from the Plaid item_id in the webhook payload.
    /// </summary>
    private async Task<Guid?> ResolveAccountId(JsonElement root, CancellationToken ct)
    {
        var plaidItemId = root.TryGetProperty("item_id", out var ii) ? ii.GetString() : null;
        if (string.IsNullOrEmpty(plaidItemId))
            return null;

        var account = await _accounts.GetByPlaidItemIdAsync(plaidItemId, ct);
        return account?.Id;
    }
}
