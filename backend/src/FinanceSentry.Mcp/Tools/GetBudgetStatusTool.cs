using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Budgets.Application.Queries;
using FinanceSentry.Modules.Budgets.API.Responses;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetBudgetStatusTool(
    IQueryHandler<GetBudgetSummaryQuery, BudgetSummaryResponse> budgetSummaryHandler,
    ILogger<GetBudgetStatusTool> logger) : IReadOnlyMcpTool
{
    private readonly IQueryHandler<GetBudgetSummaryQuery, BudgetSummaryResponse> _budgetSummaryHandler = budgetSummaryHandler;
    private readonly ILogger<GetBudgetStatusTool> _logger = logger;

    public string ToolName => "get_budget_status";

    [McpServerTool(Name = "get_budget_status")]
    [Description("Returns all active budgets for a user, including spending and utilization for the requested period.")]
    public async Task<IReadOnlyList<BudgetStatusEntry>> ExecuteAsync(
        [Description("The user's unique identifier.")] Guid userId,
        [Description("Year of the budget period (e.g. 2024). Defaults to the current year.")] int? year = null,
        [Description("Month of the budget period (1–12). Defaults to the current month.")] int? month = null,
        CancellationToken cancellationToken = default)
    {
        BudgetSummaryResponse summary;
        try
        {
            summary = await _budgetSummaryHandler.Handle(
                new GetBudgetSummaryQuery(userId, year, month),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Budget summary query unavailable for user {UserId}; returning empty list.", userId);
            return [];
        }

        var period = $"{summary.Year:D4}-{summary.Month:D2}";

        return summary.Items
            .Select(b => new BudgetStatusEntry(
                b.Id.ToString(),
                b.CategoryLabel,
                period,
                b.MonthlyLimit,
                b.Spent,
                b.Currency,
                b.MonthlyLimit > 0m
                    ? Math.Round(b.Spent / b.MonthlyLimit * 100m, 2)
                    : 0m))
            .ToList();
    }
}

public sealed record BudgetStatusEntry(
    string BudgetId,
    string Name,
    string Period,
    decimal LimitAmount,
    decimal SpentAmount,
    string Currency,
    decimal UtilizationPercent);
