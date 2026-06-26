namespace FinanceSentry.Mcp.Tools.Transactions;

using FinanceSentry.Core.Api;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

public sealed class ListTransactionsTool(IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> transactions) : IMcpTool
{
    private readonly IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> _transactions = transactions;

    public string Name => "list_transactions";
    public string Description => "Returns paginated bank transactions for the authenticated user with optional date range, account, and page size filters.";
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
            DateTime? dateFrom = null;
            DateTime? dateTo = null;
            Guid? accountId = null;
            int pageSize = 50;

            if (parameters is not null)
            {
                if (parameters.TryGetValue("dateFrom", out var dateFromStr) &&
                    DateTime.TryParse(dateFromStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedFrom))
                    dateFrom = parsedFrom;

                if (parameters.TryGetValue("dateTo", out var dateToStr) &&
                    DateTime.TryParse(dateToStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedTo))
                    dateTo = parsedTo;

                if (parameters.TryGetValue("accountId", out var accountIdStr) &&
                    Guid.TryParse(accountIdStr, out var parsedAccountId))
                    accountId = parsedAccountId;

                if (parameters.TryGetValue("pageSize", out var pageSizeStr) &&
                    int.TryParse(pageSizeStr, out var parsedPageSize) &&
                    parsedPageSize > 0)
                    pageSize = parsedPageSize;
            }

            var query = new GetAllTransactionsQuery(
                userId,
                new PagedRequest(0, pageSize),
                dateFrom,
                dateTo);

            var result = await _transactions.Handle(query, cancellationToken);

            var rows = result.Transactions.AsEnumerable();
            if (accountId.HasValue)
                rows = rows.Where(t => t.AccountId == accountId.Value);

            // currency is not present in GlobalTransactionDto — omitted per spec
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
