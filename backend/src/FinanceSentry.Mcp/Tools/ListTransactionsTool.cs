using System.ComponentModel;
using FinanceSentry.Core.Api;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BankSync.Application.Queries;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ListTransactionsTool(
    IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> transactionQueryHandler,
    IBankingAccountsReader accountsReader,
    IIdentityResolver identity,
    ILogger<ListTransactionsTool> logger)
{
    private readonly IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult> _transactionQueryHandler = transactionQueryHandler;
    private readonly IBankingAccountsReader _accountsReader = accountsReader;
    private readonly IIdentityResolver _identity = identity;
    private readonly ILogger<ListTransactionsTool> _logger = logger;

    [McpServerTool(Name = "list_transactions")]
    [Description("Returns a paginated list of bank transactions, optionally filtered by account, date range, or merchant category. Defaults to the authenticated MCP identity when userId is omitted.")]
    public async Task<IReadOnlyList<TransactionEntry>> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        [Description("Optional account ID (GUID string) to scope results to a single account.")] string? accountId = null,
        [Description("Optional inclusive start date (e.g. 2024-01-01) for filtering transactions.")] DateOnly? fromDate = null,
        [Description("Optional inclusive end date (e.g. 2024-12-31) for filtering transactions.")] DateOnly? toDate = null,
        [Description("Optional merchant category to filter on (e.g. 'FOOD_AND_DRINK'). Case-insensitive.")] string? category = null,
        [Description("1-based page number. Defaults to 1.")] int page = 1,
        [Description("Number of transactions per page. Defaults to 50.")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? _identity.GetUserId();
        if (effective is null) return [];
        var userIdVal = effective.Value;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;

        // Validate accountId format early — a non-GUID string can never match a Guid FK.
        Guid? accountGuid = null;
        if (accountId is not null)
        {
            if (!Guid.TryParse(accountId, out var parsed))
                return [];
            accountGuid = parsed;
        }

        AllTransactionsResult queryResult;
        try
        {
            queryResult = await _transactionQueryHandler.Handle(
                new GetAllTransactionsQuery(
                    userIdVal,
                    new PagedRequest(0, int.MaxValue),
                    fromDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    toDate?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BankSync transaction query unavailable for user {UserId}; returning empty list.", userId);
            return [];
        }

        Dictionary<Guid, BankingAccountSummary> accountMeta;
        try
        {
            var accounts = await _accountsReader.GetAccountSummariesAsync(userIdVal, cancellationToken);
            accountMeta = accounts.ToDictionary(a => a.AccountId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Account metadata unavailable for user {UserId}; currency and provider will be 'unknown'.", userId);
            accountMeta = [];
        }

        IEnumerable<GlobalTransactionDto> filtered = queryResult.Transactions;

        if (accountGuid.HasValue)
            filtered = filtered.Where(t => t.AccountId == accountGuid.Value);

        if (category is not null)
            filtered = filtered.Where(t => string.Equals(t.MerchantCategory, category, StringComparison.OrdinalIgnoreCase));

        int offset = (page - 1) * pageSize;

        return filtered
            .Skip(offset)
            .Take(pageSize)
            .Select(t =>
            {
                accountMeta.TryGetValue(t.AccountId, out var meta);
                return new TransactionEntry(
                    t.TransactionId.ToString(),
                    t.AccountId.ToString(),
                    t.Date,
                    t.Amount,
                    meta?.Currency ?? "unknown",
                    t.MerchantCategory,
                    t.Description,
                    meta?.Provider ?? "unknown");
            })
            .ToList();
    }
}

public sealed record TransactionEntry(
    string TransactionId,
    string AccountId,
    DateTime Date,
    decimal Amount,
    string Currency,
    string? Category,
    string Description,
    string Provider);
