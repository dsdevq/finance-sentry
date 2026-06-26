using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.Banking;

public sealed class ListBankAccountsTool(
    IQueryHandler<GetAccountsQuery, GetAccountsResult> handler) : IMcpTool
{
    public string Name => "list_bank_accounts";
    public string Description => "Returns bank accounts for the authenticated user including balance, currency, and sync status.";
    public bool IsReadOnly => true;
    public bool IsStub => false;

    public async Task<McpToolResult> InvokeAsync(
        McpToolContext context,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.Handle(
                new GetAccountsQuery(context.UserId),
                cancellationToken);

            var payload = new
            {
                totalCount = result.TotalCount,
                accounts = result.Accounts.Select(a => new
                {
                    accountId = a.AccountId,
                    bankName = a.BankName,
                    accountType = a.AccountType,
                    currency = a.Currency,
                    currentBalance = a.CurrentBalance,
                    syncStatus = a.SyncStatus,
                    provider = a.Provider,
                }).ToList(),
            };

            return McpToolResult.Success(payload);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }
}
