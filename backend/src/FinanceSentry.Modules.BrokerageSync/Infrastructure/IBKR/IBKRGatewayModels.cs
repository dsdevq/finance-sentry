using Newtonsoft.Json;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed record IBKRAuthInitRequest(
    [property: JsonProperty("username")] string Username,
    [property: JsonProperty("password")] string Password);

public sealed record IBKRAuthStatusResponse(
    [property: JsonProperty("authenticated")] bool Authenticated,
    [property: JsonProperty("connected")] bool Connected);

public sealed record IBKRAccountsResponse(
    [property: JsonProperty("accounts")] List<string> Accounts);

public sealed record IBKRPositionResponse(
    [property: JsonProperty("conid")] long Conid,
    [property: JsonProperty("contractDesc")] string ContractDesc,
    [property: JsonProperty("assetClass")] string AssetClass,
    [property: JsonProperty("position")] decimal Position,
    [property: JsonProperty("mktPrice")] decimal MktPrice,
    [property: JsonProperty("mktValue")] decimal MktValue);
