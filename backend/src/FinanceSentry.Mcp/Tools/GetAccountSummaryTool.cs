using System.ComponentModel;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Abstractions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetAccountSummaryTool(
    IBankingAccountsReader bankingReader,
    ICryptoHoldingsReader cryptoReader,
    IBrokerageHoldingsReader brokerageReader,
    ILogger<GetAccountSummaryTool> logger) : IReadOnlyMcpTool
{
    private readonly IBankingAccountsReader _bankingReader = bankingReader;
    private readonly ICryptoHoldingsReader _cryptoReader = cryptoReader;
    private readonly IBrokerageHoldingsReader _brokerageReader = brokerageReader;
    private readonly ILogger<GetAccountSummaryTool> _logger = logger;

    public string ToolName => "get_account_summary";

    [McpServerTool(Name = "get_account_summary")]
    [Description("Returns a consolidated account summary across banking, crypto, and brokerage providers for a given user.")]
    public async Task<IReadOnlyList<AccountSummaryEntry>> ExecuteAsync(
        [Description("The user's unique identifier.")] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AccountSummaryEntry>();

        // Banking accounts — each account is one entry.
        try
        {
            var accounts = await _bankingReader.GetAccountSummariesAsync(userId, cancellationToken);
            results.AddRange(accounts.Select(a => new AccountSummaryEntry(
                a.AccountId.ToString(),
                a.BankName,
                a.Provider,
                a.Currency,
                a.CurrentBalance ?? 0m)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BankSync provider unavailable for user {UserId}; contributing empty list.", userId);
        }

        // Crypto holdings — each asset is one entry denominated in USD.
        try
        {
            var holdings = await _cryptoReader.GetHoldingsAsync(userId, cancellationToken);
            results.AddRange(holdings.Select(h => new AccountSummaryEntry(
                h.Asset,
                h.Asset,
                h.Provider,
                "USD",
                h.UsdValue)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CryptoSync provider unavailable for user {UserId}; contributing empty list.", userId);
        }

        // Brokerage positions — each position is one entry denominated in USD.
        try
        {
            var positions = await _brokerageReader.GetHoldingsAsync(userId, cancellationToken);
            results.AddRange(positions.Select(h => new AccountSummaryEntry(
                h.Symbol,
                h.Symbol,
                h.Provider,
                "USD",
                h.UsdValue)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BrokerageSync provider unavailable for user {UserId}; contributing empty list.", userId);
        }

        return results;
    }
}

public sealed record AccountSummaryEntry(
    string AccountId,
    string Name,
    string Provider,
    string Currency,
    decimal Balance);
