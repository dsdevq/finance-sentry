using System.ComponentModel;
using FinanceSentry.Core.Api;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetCashflowReportTool(
    IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> transactionsHandler,
    IIdentityResolver identity,
    ILogger<GetCashflowReportTool> logger)
{
    private const int MaxMonthsBack = 24;
    private const int DefaultMonthsBack = 6;

    private readonly IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> _transactionsHandler = transactionsHandler;
    private readonly IIdentityResolver _identity = identity;
    private readonly ILogger<GetCashflowReportTool> _logger = logger;

    [McpServerTool(Name = "get_cashflow_report")]
    [Description("Returns a monthly cashflow report (inflow, outflow, net) aggregated from bank transactions. Negative amounts are treated as outflows, positive as inflows. Defaults to the authenticated MCP identity when userId is omitted.")]
    public async Task<IReadOnlyList<CashflowReportEntry>> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        [Description("Optional inclusive start date. Defaults to 6 months ago.")] DateOnly? fromDate = null,
        [Description("Optional inclusive end date. Defaults to today.")] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? _identity.GetUserId();
        if (effective is null) return [];
        var userIdVal = effective.Value;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var to = toDate ?? today;
        var from = fromDate ?? to.AddMonths(-DefaultMonthsBack);

        if (from > to)
            return [];

        if (to.DayNumber - from.DayNumber > MaxMonthsBack * 31)
            from = to.AddMonths(-MaxMonthsBack);

        AllTransactionsResult result;
        try
        {
            result = await _transactionsHandler.Handle(
                new GetAllTransactionsQuery(
                    userIdVal,
                    new PagedRequest(0, int.MaxValue),
                    from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transactions query unavailable for user {UserId}; returning empty cashflow report.", userIdVal);
            return [];
        }

        // Plaid + Monobank both store Amount as a positive magnitude. The direction
        // lives in TransactionType ("credit" = inflow, "debit" = outflow). Sign of
        // Amount is NOT used.
        return result.Transactions
            .GroupBy(t => new { (t.PostedDate ?? t.Date).Year, (t.PostedDate ?? t.Date).Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var inflow = g
                    .Where(t => IsCredit(t.TransactionType))
                    .Sum(t => Math.Abs(t.Amount));
                var outflow = g
                    .Where(t => IsDebit(t.TransactionType))
                    .Sum(t => Math.Abs(t.Amount));
                return new CashflowReportEntry(
                    $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    inflow,
                    outflow,
                    inflow - outflow,
                    g.Count());
            })
            .ToList();
    }

    private static bool IsCredit(string? transactionType) =>
        string.Equals(transactionType, "credit", StringComparison.OrdinalIgnoreCase);

    private static bool IsDebit(string? transactionType) =>
        string.Equals(transactionType, "debit", StringComparison.OrdinalIgnoreCase);
}

public sealed record CashflowReportEntry(
    string Period,
    decimal Inflow,
    decimal Outflow,
    decimal Net,
    int TransactionCount);
