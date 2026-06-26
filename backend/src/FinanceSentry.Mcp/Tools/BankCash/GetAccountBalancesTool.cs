namespace FinanceSentry.Mcp.Tools.BankCash;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

public sealed class GetAccountBalancesTool(IQueryHandler<GetAccountsQuery, GetAccountsResult> accounts) : IMcpTool
{
    private readonly IQueryHandler<GetAccountsQuery, GetAccountsResult> _accounts = accounts;

    public string Name => "get_account_balances";
    public string Description => "Returns current balance and currency for each bank account for the authenticated user.";
    public bool IsReadOnly => true;
    public bool IsStub => false;
    public string? StubReason => null;

    public async Task<McpToolResult> InvokeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _accounts.Handle(new GetAccountsQuery(userId), cancellationToken);
            var payload = result.Accounts.Select(a => new
            {
                accountId = a.AccountId,
                balance = a.CurrentBalance,
                currency = a.Currency,
            });
            return McpToolResult.Success(payload);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
