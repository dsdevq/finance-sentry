namespace FinanceSentry.Modules.BankSync.API.Controllers;

using FinanceSentry.Core.Auth;
using FinanceSentry.Modules.BankSync.API.Responses;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("dashboard")]
public class DashboardController(
    IDashboardQueryService dashboard,
    ITransactionRepository transactions,
    ITransferDetectionService transferDetection) : ControllerBase
{
    private readonly IDashboardQueryService _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
    private readonly ITransactionRepository _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    private readonly ITransferDetectionService _transferDetection = transferDetection ?? throw new ArgumentNullException(nameof(transferDetection));

    // ── GET /api/dashboard/aggregated ── T408 ─────────────────────────────────

    [HttpGet("aggregated")]
    public async Task<IActionResult> GetAggregated(CancellationToken ct)
    {
        var data = await _dashboard.GetDashboardDataAsync(User.RequireUserId(), ct);
        return Ok(data);
    }

    // ── GET /api/dashboard/transfers ── T410 ──────────────────────────────────

    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers(CancellationToken ct)
    {
        var allTx = (await _transactions.GetByUserIdAsync(User.RequireUserId(), ct)).ToList();

        var debits = allTx.Where(t => t.TransactionType == "debit" && !t.IsPending).ToList();
        var credits = allTx.Where(t => t.TransactionType == "credit" && !t.IsPending).ToList();

        var pairs = new List<TransferPairDto>();

        foreach (var debit in debits)
        {
            foreach (var credit in credits)
            {
                if (_transferDetection.IsLikelyTransfer(debit, credit))
                {
                    pairs.Add(new TransferPairDto(
                        new TransferItemDto(
                            debit.Id, debit.AccountId, debit.Amount,
                            debit.PostedDate ?? debit.TransactionDate, debit.Description),
                        new TransferItemDto(
                            credit.Id, credit.AccountId, credit.Amount,
                            credit.PostedDate ?? credit.TransactionDate, credit.Description)));
                }
            }
        }

        return Ok(new TransferPairsResponse(pairs, pairs.Count));
    }
}
