namespace FinanceSentry.Modules.BankSync.API.Controllers;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Dashboard endpoints — aggregated balance, money flow, category stats, and transfer detection.
/// All endpoints are user-scoped (FR-009).
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardQueryService _dashboard;
    private readonly ITransactionRepository _transactions;
    private readonly ITransferDetectionService _transferDetection;

    public DashboardController(
        IDashboardQueryService dashboard,
        ITransactionRepository transactions,
        ITransferDetectionService transferDetection)
    {
        _dashboard         = dashboard         ?? throw new ArgumentNullException(nameof(dashboard));
        _transactions      = transactions      ?? throw new ArgumentNullException(nameof(transactions));
        _transferDetection = transferDetection ?? throw new ArgumentNullException(nameof(transferDetection));
    }

    // ── GET /api/dashboard/aggregated ── T408 ─────────────────────────────────

    /// <summary>
    /// Returns the full dashboard payload for the user:
    /// aggregated balances, account counts, monthly flow, top categories, last sync timestamp.
    /// </summary>
    [HttpGet("aggregated")]
    public async Task<IActionResult> GetAggregated([FromQuery] Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { error = "userId is required." });

        var data = await _dashboard.GetDashboardDataAsync(userId, ct);

        return Ok(new
        {
            aggregatedBalance  = data.AggregatedBalance,
            accountCount       = data.AccountCount,
            accountsByType     = data.AccountsByType,
            monthlyFlow        = data.MonthlyFlow,
            topCategories      = data.TopCategories,
            lastSyncTimestamp  = data.LastSyncTimestamp
        });
    }

    // ── GET /api/dashboard/transfers ── T410 ──────────────────────────────────

    /// <summary>
    /// Returns pairs of transactions that are likely internal transfers between accounts.
    /// </summary>
    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers([FromQuery] Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { error = "userId is required." });

        var allTx = (await _transactions.GetByUserIdAsync(userId, ct)).ToList();

        // Separate debits and credits that are candidates for transfer pairing
        var debits  = allTx.Where(t => t.TransactionType == "debit"  && !t.IsPending).ToList();
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
                            accountId     = debit.AccountId,
                            amount        = debit.Amount,
                            date          = debit.PostedDate ?? debit.TransactionDate,
                            description   = debit.Description
                        },
                        credit = new
                        {
                            transactionId = credit.Id,
                            accountId     = credit.AccountId,
                            amount        = credit.Amount,
                            date          = credit.PostedDate ?? credit.TransactionDate,
                            description   = credit.Description
                        }
                    });
                }
            }
        }

        return Ok(new { transfers = pairs, count = pairs.Count });
    }
}
