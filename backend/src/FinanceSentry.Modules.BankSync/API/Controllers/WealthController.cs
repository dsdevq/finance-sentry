namespace FinanceSentry.Modules.BankSync.API.Controllers;

using System.Security.Claims;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Application.Queries;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("wealth")]
public class WealthController(
    IQueryHandler<GetWealthSummaryQuery, WealthSummaryResponse> wealthSummaryHandler,
    IQueryHandler<GetTransactionSummaryQuery, TransactionSummaryResponse> txSummaryHandler) : ControllerBase
{
    private static readonly HashSet<string> AllowedCategories =
        new(StringComparer.OrdinalIgnoreCase) { "banking", "crypto", "brokerage", "other" };

    private Guid? GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? category,
        [FromQuery] string? provider,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        if (category is not null && !AllowedCategories.Contains(category))
            return BadRequest(new
            {
                error = "Invalid category value. Allowed: banking, crypto, brokerage, other.",
                errorCode = "INVALID_FILTER"
            });

        var result = await wealthSummaryHandler.Handle(new GetWealthSummaryQuery(userId.Value, category, provider), ct);
        return Ok(result);
    }

    [HttpGet("transactions/summary")]
    public async Task<IActionResult> GetTransactionSummary(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? category,
        [FromQuery] string? provider,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return BadRequest(new
            {
                error = "Query parameters 'from' and 'to' are required.",
                errorCode = "MISSING_DATE_RANGE"
            });

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate)
         || !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
            return BadRequest(new
            {
                error = "Parameters 'from' and 'to' must be in yyyy-MM-dd format.",
                errorCode = "INVALID_DATE_RANGE"
            });

        if (fromDate > toDate)
            return BadRequest(new
            {
                error = "Parameter 'from' must be less than or equal to 'to'.",
                errorCode = "INVALID_DATE_RANGE"
            });

        if (category is not null && !AllowedCategories.Contains(category))
            return BadRequest(new
            {
                error = "Invalid category value. Allowed: banking, crypto, brokerage, other.",
                errorCode = "INVALID_FILTER"
            });

        var result = await txSummaryHandler.Handle(
            new GetTransactionSummaryQuery(userId.Value, fromDate, toDate, category, provider), ct);
        return Ok(result);
    }
}
