using System.Text.Json.Serialization;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;

public sealed record BinanceAccountResponse(
    [property: JsonPropertyName("balances")] IReadOnlyList<BinanceBalance> Balances);

public sealed record BinanceBalance(
    [property: JsonPropertyName("asset")] string Asset,
    [property: JsonPropertyName("free")] string Free,
    [property: JsonPropertyName("locked")] string Locked);

public sealed record BinancePriceTicker(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("price")] string Price);

public sealed record BinanceErrorResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("msg")] string Message);

// /sapi/v1/asset/get-funding-asset — flat list of per-asset balances in the Funding wallet.
public sealed record BinanceFundingAsset(
    [property: JsonPropertyName("asset")] string Asset,
    [property: JsonPropertyName("free")] string Free,
    [property: JsonPropertyName("locked")] string? Locked,
    [property: JsonPropertyName("freeze")] string? Freeze,
    [property: JsonPropertyName("withdrawing")] string? Withdrawing);

// Generic paginated wrapper for the Simple Earn endpoints.
public sealed record BinanceEarnPage<T>(
    [property: JsonPropertyName("rows")] IReadOnlyList<T> Rows,
    [property: JsonPropertyName("total")] int Total);

// /sapi/v1/simple-earn/flexible/position — Simple Earn Flexible product positions.
public sealed record BinanceFlexibleEarnPosition(
    [property: JsonPropertyName("asset")] string Asset,
    [property: JsonPropertyName("totalAmount")] string TotalAmount);

// /sapi/v1/simple-earn/locked/position — Simple Earn Locked product positions.
public sealed record BinanceLockedEarnPosition(
    [property: JsonPropertyName("asset")] string Asset,
    [property: JsonPropertyName("amount")] string Amount);
