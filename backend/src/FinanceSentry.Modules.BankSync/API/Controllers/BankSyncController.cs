namespace FinanceSentry.Modules.BankSync.API.Controllers;

using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Application.Commands;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("accounts")]
public class BankSyncController(
    ICommandHandler<ConnectBankAccountCommand, ConnectBankAccountResult> connectHandler,
    ICommandHandler<ConnectMonobankAccountCommand, ConnectMonobankResult> connectMonobankHandler,
    IQueryHandler<GetAccountsQuery, GetAccountsResult> getAccountsHandler,
    PlaidAdapter plaid,
    IBankAccountRepository accounts,
    ITransactionRepository transactions,
    IBackgroundJobClient backgroundJobs,
    ISyncJobRepository syncJobs,
    ITransactionSyncCoordinator coordinator) : ControllerBase
{
    private readonly PlaidAdapter _plaid = plaid;
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
    private readonly ISyncJobRepository _syncJobs = syncJobs;
    private readonly ITransactionSyncCoordinator _coordinator = coordinator;

    // ── POST /api/accounts/connect ── T205 ───────────────────────────────────

    [HttpPost("connect")]
    public async Task<IActionResult> Connect(CancellationToken ct)
    {
        var result = await _plaid.CreateLinkTokenAsync(User.RequireUserId(), ct);
        return Ok(new
        {
            linkToken = result.LinkToken,
            expiresIn = (int)result.ExpiresIn.TotalSeconds,
            requestId = result.RequestId
        });
    }

    // ── POST /api/accounts/link ── T206 ──────────────────────────────────────

    [HttpPost("link")]
    public async Task<IActionResult> Link([FromBody] LinkRequest request, CancellationToken ct)
    {
        var result = await connectHandler.Handle(new ConnectBankAccountCommand(
            User.RequireUserId(), request.PublicToken, request.InstitutionName), ct);

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

    [HttpGet]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] string? status = null,
        [FromQuery] string? currency = null,
        CancellationToken ct = default)
    {
        var result = await getAccountsHandler.Handle(
            new GetAccountsQuery(User.RequireUserId(), status, currency), ct);

        return Ok(new
        {
            accounts = result.Accounts,
            totalCount = result.TotalCount,
            currencyTotals = result.CurrencyTotals
        });
    }

    // ── GET /api/accounts/{accountId}/transactions ── T208 ───────────────────

    [HttpGet("{accountId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid accountId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var userId = User.RequireUserId();

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

    [HttpPost("{accountId:guid}/sync")]
    public async Task<IActionResult> TriggerSync(
        Guid accountId,
        CancellationToken ct)
    {
        var userId = User.RequireUserId();

        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.UserId != userId)
            return NotFound(new { error = "Account not found." });

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

    [HttpGet("{accountId:guid}/sync-status")]
    public async Task<IActionResult> GetSyncStatus(
        Guid accountId,
        CancellationToken ct)
    {
        var userId = User.RequireUserId();

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

    [HttpDelete("{accountId:guid}")]
    public async Task<IActionResult> DeleteAccount(
        Guid accountId,
        CancellationToken ct)
    {
        var userId = User.RequireUserId();

        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.UserId != userId)
            return NotFound(new { error = "Account not found." });

        await _transactions.SoftDeleteByAccountIdAsync(accountId, ct);
        await _accounts.DeleteAsync(accountId, ct);

        return NoContent();
    }

    // ── POST /api/accounts/monobank/connect ── T019 ──────────────────────────

    [HttpPost("monobank/connect")]
    public async Task<IActionResult> ConnectMonobank(
        [FromBody] ConnectMonobankRequest request, CancellationToken ct)
    {
        var result = await connectMonobankHandler.Handle(
            new ConnectMonobankAccountCommand(User.RequireUserId(), request.Token), ct);

        return StatusCode(201, new { accounts = result.Accounts });
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public record LinkRequest(string PublicToken, string InstitutionName);
public record ConnectMonobankRequest(string Token);
