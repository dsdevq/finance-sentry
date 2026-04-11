namespace FinanceSentry.Modules.BankSync.API.Controllers;

using System.Security.Claims;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Dashboard endpoints — aggregated balance, money flow, category stats, and transfer detection.
/// All endpoints are user-scoped (FR-009). userId is extracted from the JWT claim `sub`.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IDashboardQueryService dashboard,
    ITransactionRepository transactions,
    ITransferDetectionService transferDetection) : ControllerBase
{
    private readonly IDashboardQueryService _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly ITransferDetectionService _transferDetection = transferDetection ?? throw new ArgumentNullException(nameof(transferDetection));

    private Guid? GetUserIdFromClaims()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    // ── GET /api/dashboard/aggregated ── T408 ─────────────────────────────────

    /// <summary>
    /// Returns the full dashboard payload for the user:
    /// aggregated balances, account counts, monthly flow, top categories, last sync timestamp.
    /// </summary>
    [HttpGet("aggregated")]
    public async Task<IActionResult> GetAggregated(CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        var data = await _dashboard.GetDashboardDataAsync(userId.Value, ct);

        return Ok(new
        {
            aggregatedBalance = data.AggregatedBalance,
            accountCount = data.AccountCount,
            accountsByType = data.AccountsByType,
            monthlyFlow = data.MonthlyFlow,
            topCategories = data.TopCategories,
            lastSyncTimestamp = data.LastSyncTimestamp
        });
    }

    // ── GET /api/dashboard/transfers ── T410 ──────────────────────────────────

    /// <summary>
    /// Returns pairs of transactions that are likely internal transfers between accounts.
    /// </summary>
    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers(CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        var allTx = (await _transactions.GetByUserIdAsync(userId.Value, ct)).ToList();

        // Separate debits and credits that are candidates for transfer pairing
        var debits = allTx.Where(t => t.TransactionType == "debit" && !t.IsPending).ToList();
        var credits = allTx.Where(t => t.TransactionType == "credit" && !t.IsPending).ToList();

        var pairs = new List<object>();

        foreach (var debit in debits)
        {
            foreach (var credit in credits)
            {
                if (_transferDetection.IsLikelyTransfer(debit, credit))
                {
                    pairs.Add(new
                    {
                        debit = new
                        {
                            transactionId = debit.Id,
                            accountId = debit.AccountId,
                            amount = debit.Amount,
                            date = debit.PostedDate ?? debit.TransactionDate,
                            description = debit.Description
                        },
                        credit = new
                        {
                            transactionId = credit.Id,
                            accountId = credit.AccountId,
                            amount = credit.Amount,
                            date = credit.PostedDate ?? credit.TransactionDate,
                            description = credit.Description
                        }
                    });
                }
            }
        }

        return Ok(new { transfers = pairs, count = pairs.Count });
    }
}
