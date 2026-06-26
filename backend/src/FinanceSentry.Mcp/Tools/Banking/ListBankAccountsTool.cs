using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.Banking;

public sealed class ListBankAccountsTool(
    IQueryHandler<GetAccountsQuery, GetAccountsResult> accounts) : IMcpTool
{
    public string Name => "list_bank_accounts";
    public string Description => "Lists all bank accounts for the authenticated user.";
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
            var result = await accounts.Handle(new GetAccountsQuery(userId), cancellationToken);
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
