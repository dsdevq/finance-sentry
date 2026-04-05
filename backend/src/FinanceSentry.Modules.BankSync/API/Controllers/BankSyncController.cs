namespace FinanceSentry.Modules.BankSync.API.Controllers;

using FinanceSentry.Modules.BankSync.Application.Commands;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// REST endpoints for Bank Account Sync (US1 + US2).
/// All endpoints are user-scoped — FR-009.
/// </summary>
[ApiController]
[Route("api/accounts")]
public class BankSyncController(
    IMediator mediator,
    PlaidAdapter plaid,
    IBankAccountRepository accounts,
    ITransactionRepository transactions,
    IBackgroundJobClient backgroundJobs,
    ISyncJobRepository syncJobs,
    ITransactionSyncCoordinator coordinator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly PlaidAdapter _plaid = plaid;
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
    private readonly ISyncJobRepository _syncJobs = syncJobs;
    private readonly ITransactionSyncCoordinator _coordinator = coordinator;

    // ── POST /api/accounts/connect ── T205 ───────────────────────────────────

    /// <summary>Step 1: Get a Plaid Link token to open Plaid Link in the frontend.</summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectRequest request, CancellationToken ct)
    {
        var result = await _plaid.CreateLinkTokenAsync(request.UserId, ct);
        return Ok(new
        {
            linkToken = result.LinkToken,
            expiresIn = (int)result.ExpiresIn.TotalSeconds,
            requestId = result.RequestId
        });
    }

    // ── POST /api/accounts/link ── T206 ──────────────────────────────────────

    /// <summary>Step 2: Exchange public token and store encrypted access token.</summary>
    [HttpPost("link")]
    public async Task<IActionResult> Link([FromBody] LinkRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConnectBankAccountCommand(
            request.UserId, request.PublicToken, request.InstitutionName), ct);

        return Ok(new
        {
            accountId = result.AccountId,
            bankName = result.BankName,
            accountType = result.AccountType,
            accountNumberLast4 = result.AccountNumberLast4,
            currency = result.Currency,
            syncStatus = result.SyncStatus,
            message = "Account linked. Syncing transaction history..."
        });
    }

    // ── GET /api/accounts ── T207 ────────────────────────────────────────────

    /// <summary>Returns all accounts for the authenticated user. FR-009: scoped to userId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] Guid userId,
        [FromQuery] string? status = null,
        [FromQuery] string? currency = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAccountsQuery(userId, status, currency), ct);

        return Ok(new
        {
            accounts = result.Accounts,
            totalCount = result.TotalCount,
            currencyTotals = result.CurrencyTotals
        });
    }

    // ── GET /api/accounts/{accountId}/transactions ── T208 ───────────────────

    /// <summary>Returns paginated transactions for an account. FR-009: 404 if not owned by user.</summary>
    [HttpGet("{accountId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid accountId,
        [FromQuery] Guid userId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        // FR-009: verify ownership before returning data
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.UserId != userId)
            return NotFound(new { error = "Account not found." });

        var transactions = (await _transactions.GetByAccountIdAsync(accountId, offset, limit, ct)).ToList();
        var totalCount = await _transactions.CountByAccountIdAsync(accountId, ct);

        return Ok(new
        {
            transactions = transactions.Select(t => new
            {
                transactionId = t.Id,
                amount = t.Amount,
                date = t.TransactionDate,
                postedDate = t.PostedDate,
                description = t.Description,
                transactionType = t.TransactionType,
                merchantCategory = t.MerchantCategory,
                isPending = t.IsPending,
                createdAt = t.CreatedAt
            }),
            totalCount,
            hasMore = (offset + limit) < totalCount
        });
    }

    // ── POST /api/accounts/{accountId}/sync ── T308 ──────────────────────────

    /// <summary>
    /// Manually triggers an asynchronous sync for the given account.
    /// Returns 202 Accepted with the Hangfire job ID.
    /// Returns 409 Conflict if a sync is already running.
    /// </summary>
    [HttpPost("{accountId:guid}/sync")]
    public async Task<IActionResult> TriggerSync(
        Guid accountId,
        [FromQuery] Guid userId,
        CancellationToken ct)
    {
        // FR-009: ownership check
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.UserId != userId)
            return NotFound(new { error = "Account not found." });

        // Idempotency: reject if sync already running
        if (await _syncJobs.HasRunningJobAsync(accountId, ct))
            return Conflict(new { error = "A sync is already in progress for this account." });

        var hangfireJobId = _backgroundJobs.Enqueue<Infrastructure.Jobs.ScheduledSyncJob>(
            job => job.ExecuteSyncAsync(accountId));

        return Accepted(new
        {
            jobId = hangfireJobId,
            message = "Sync enqueued. Use GET /api/accounts/{accountId}/sync-status to track progress."
        });
    }

    // ── GET /api/accounts/{accountId}/sync-status ── T309 ───────────────────

    /// <summary>
    /// Returns the latest sync job status for the given account.
    /// </summary>
    [HttpGet("{accountId:guid}/sync-status")]
    public async Task<IActionResult> GetSyncStatus(
        Guid accountId,
        [FromQuery] Guid userId,
        CancellationToken ct)
    {
        // FR-009: ownership check
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.UserId != userId)
            return NotFound(new { error = "Account not found." });

        var latestJob = await _syncJobs.GetLatestByAccountIdAsync(accountId, ct);
        if (latestJob == null)
            return Ok(new { status = "never_synced", message = "No sync history for this account." });

        return Ok(new
        {
            status = latestJob.Status,
            transactionCountFetched = latestJob.TransactionCountFetched,
            transactionCountDeduped = latestJob.TransactionCountDeduped,
            errorMessage = latestJob.ErrorMessage,
            lastSyncTimestamp = latestJob.CompletedAt,
            startedAt = latestJob.StartedAt,
            webhookTriggered = latestJob.WebhookTriggered
        });
    }

    // ── DELETE /api/accounts/{accountId} ── T309-A ───────────────────────────

    /// <summary>
    /// Soft-deletes the bank account and all associated transactions.
    /// Account and transactions are marked IsActive=false for audit trail preservation.
    /// </summary>
    [HttpDelete("{accountId:guid}")]
    public async Task<IActionResult> DeleteAccount(
        Guid accountId,
        [FromQuery] Guid userId,
        CancellationToken ct)
    {
        // FR-009: ownership check
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.UserId != userId)
            return NotFound(new { error = "Account not found." });

        // Soft-delete all transactions for the account
        await _transactions.SoftDeleteByAccountIdAsync(accountId, ct);

        // Soft-delete the account itself
        await _accounts.DeleteAsync(accountId, ct);

        return NoContent();
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public record ConnectRequest(Guid UserId);

public record LinkRequest(Guid UserId, string PublicToken, string InstitutionName);
