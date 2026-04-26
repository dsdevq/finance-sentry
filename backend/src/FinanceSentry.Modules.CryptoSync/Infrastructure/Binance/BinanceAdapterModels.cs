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
