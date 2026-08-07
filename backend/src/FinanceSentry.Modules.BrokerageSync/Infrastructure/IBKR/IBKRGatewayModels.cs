using System.Text.Json.Serialization;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// One entry of the <c>/v1/api/portfolio/accounts</c> response — the tier-1
/// (read-only Portal session) account list. Unlike <c>/iserver/accounts</c>
/// this endpoint needs no brokerage session, so it works for read-only IBKR
/// users who can never open a tier-2 <c>/iserver</c> session.
/// </summary>
public sealed record IBKRPortfolioAccountResponse(
    [property: JsonPropertyName("accountId")] string? AccountId,
    [property: JsonPropertyName("id")] string? Id);

public sealed record IBKRAccountsResponse(
    [property: JsonPropertyName("accounts")] List<string> Accounts);

public sealed record IBKRPositionResponse(
    [property: JsonPropertyName("conid")] long Conid,
    [property: JsonPropertyName("contractDesc")] string ContractDesc,
    [property: JsonPropertyName("assetClass")] string AssetClass,
    [property: JsonPropertyName("position")] decimal Position,
    [property: JsonPropertyName("mktPrice")] decimal MktPrice,
    [property: JsonPropertyName("mktValue")] decimal MktValue,
    [property: JsonPropertyName("avgCost")] decimal? AvgCost = null,
    [property: JsonPropertyName("avgPrice")] decimal? AvgPrice = null);

/// <summary>
/// One currency row of <c>/v1/api/portfolio/{accountId}/ledger</c> — the account's settled cash
/// per currency. The response is keyed by currency code; the pseudo-key <c>"BASE"</c> aggregates
/// every currency in the account's base currency, so it is skipped to avoid double-counting.
/// </summary>
public sealed record IBKRLedgerEntry(
    [property: JsonPropertyName("cashbalance")] decimal CashBalance,
    [property: JsonPropertyName("currency")] string? Currency = null);
