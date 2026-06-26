namespace FinanceSentry.Mcp.Tools.BankCash;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

public sealed class ListBankAccountsTool(IQueryHandler<GetAccountsQuery, GetAccountsResult> accounts) : IMcpTool
{
    private readonly IQueryHandler<GetAccountsQuery, GetAccountsResult> _accounts = accounts;

    public string Name => "list_bank_accounts";
    public string Description => "Lists all bank accounts for the authenticated user, including name, type, currency, and sync status.";
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
                bankName = a.BankName,
                accountType = a.AccountType,
                currency = a.Currency,
                syncStatus = a.SyncStatus,
                provider = a.Provider,
            });
            return McpToolResult.Success(payload);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
