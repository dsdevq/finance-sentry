using System.Text.Json.Serialization;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed record IBKRAuthInitRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password);

public sealed record IBKRAuthStatusResponse(
    [property: JsonPropertyName("authenticated")] bool Authenticated,
    [property: JsonPropertyName("connected")] bool Connected);

public sealed record IBKRAccountsResponse(
    [property: JsonPropertyName("accounts")] List<string> Accounts);

public sealed record IBKRPositionResponse(
    [property: JsonPropertyName("conid")] long Conid,
    [property: JsonPropertyName("contractDesc")] string ContractDesc,
    [property: JsonPropertyName("assetClass")] string AssetClass,
    [property: JsonPropertyName("position")] decimal Position,
    [property: JsonPropertyName("mktPrice")] decimal MktPrice,
    [property: JsonPropertyName("mktValue")] decimal MktValue);
