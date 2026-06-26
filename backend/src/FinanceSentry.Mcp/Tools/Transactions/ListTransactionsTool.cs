using FinanceSentry.Core.Api;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.Transactions;

public sealed class ListTransactionsTool(
    IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> transactions) : IMcpTool
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public string Name => "list_transactions";
    public string Description => "Returns paginated bank transactions for the authenticated user.";
    public bool IsReadOnly => true;
    public bool IsStub => false;
    public string? StubReason => null;

    public async Task<McpToolResult> InvokeAsync(
        Guid userId,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pageSize = DefaultPageSize;
            if (parameters?.TryGetValue("pageSize", out var pageSizeStr) == true
                && int.TryParse(pageSizeStr, out var parsed))
            {
                pageSize = Math.Clamp(parsed, 1, MaxPageSize);
            }

            DateTime? dateFrom = null;
            if (parameters?.TryGetValue("dateFrom", out var dateFromStr) == true
                && DateTime.TryParse(dateFromStr, out var parsedFrom))
            {
                dateFrom = parsedFrom;
            }

            DateTime? dateTo = null;
            if (parameters?.TryGetValue("dateTo", out var dateToStr) == true
                && DateTime.TryParse(dateToStr, out var parsedTo))
            {
                dateTo = parsedTo;
            }

            var query = new GetAllTransactionsQuery(userId, new PagedRequest(0, pageSize), dateFrom, dateTo);
            var result = await transactions.Handle(query, cancellationToken);

            IEnumerable<GlobalTransactionDto> rows = result.Transactions;
            if (parameters?.TryGetValue("accountId", out var accountIdStr) == true
                && Guid.TryParse(accountIdStr, out var accountId))
            {
                rows = rows.Where(t => t.AccountId == accountId);
            }

            var payload = rows.Select(t => new
            {
                transactionId = t.TransactionId,
                accountId = t.AccountId,
                date = t.Date,
                description = t.Description,
                amount = t.Amount,
                category = t.MerchantCategory,
            });
            return McpToolResult.Success(payload);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
