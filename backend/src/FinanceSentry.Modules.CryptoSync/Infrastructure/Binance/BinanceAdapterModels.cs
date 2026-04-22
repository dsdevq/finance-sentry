using Newtonsoft.Json;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;

public sealed record BinanceAccountResponse(
    [property: JsonProperty("balances")] IReadOnlyList<BinanceBalance> Balances);

public sealed record BinanceBalance(
    [property: JsonProperty("asset")] string Asset,
    [property: JsonProperty("free")] string Free,
    [property: JsonProperty("locked")] string Locked);

public sealed record BinancePriceTicker(
    [property: JsonProperty("symbol")] string Symbol,
    [property: JsonProperty("price")] string Price);

public sealed record BinanceErrorResponse(
    [property: JsonProperty("code")] int Code,
    [property: JsonProperty("msg")] string Message);
