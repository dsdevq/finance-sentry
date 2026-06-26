using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;

namespace FinanceSentry.Mcp.Tools.BankCash;

public sealed class ListBankAccountsTool(IQueryHandler<GetAccountsQuery, GetAccountsResult> handler) : IMcpTool
{
    public string Name => "list_bank_accounts";

    public string Description => "Returns all bank accounts for the authenticated user from the BankSync module.";

    public bool IsReadOnly => true;

    public bool IsStub => false;

    public async Task<McpToolResult<object>> InvokeAsync(
        IReadOnlyDictionary<string, object?> args,
        McpToolContext context,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.Handle(new GetAccountsQuery(context.UserId), ct);
            var accounts = result.Accounts.Select(a => new BankAccountPayload(
                a.AccountId,
                $"{a.BankName} ****{a.AccountNumberLast4}",
                a.AccountType,
                a.Currency,
                a.BankName));
            return McpToolResult.Success(new { accounts });
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }

    private record BankAccountPayload(
        Guid AccountId,
        string Name,
        string Type,
        string Currency,
        string Institution);
}
