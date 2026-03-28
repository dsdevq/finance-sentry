namespace FinanceSentry.Modules.BankSync.API.Controllers;

using FinanceSentry.Modules.BankSync.Application.Commands;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// REST endpoints for Bank Account Sync (US1).
/// All endpoints are user-scoped — FR-009.
/// </summary>
[ApiController]
[Route("api/accounts")]
public class BankSyncController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PlaidAdapter _plaid;
    private readonly IBankAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public BankSyncController(IMediator mediator, PlaidAdapter plaid,
        IBankAccountRepository accounts, ITransactionRepository transactions)
    {
        _mediator = mediator;
        _plaid = plaid;
        _accounts = accounts;
        _transactions = transactions;
    }

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
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public record ConnectRequest(Guid UserId);

public record LinkRequest(Guid UserId, string PublicToken, string InstitutionName);
