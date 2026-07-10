namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Configuration for the per-user IBeam Docker orchestration.
///
/// The API container spawns one IBeam container per connected user, joined to
/// <see cref="Network"/> with a stable name so the API can address it by DNS
/// at <c>https://finance-sentry-ibkr-{shortId}:5000</c>.
///
/// <see cref="Image"/> points at our custom build (docker/Dockerfile.ibeam),
/// not upstream <c>voyz/ibeam:latest</c>. The upstream image ships a CPG
/// conf.yaml that allowlists only <c>172.17.0.*</c> (Docker's default bridge
/// subnet); our conf.yaml widens that to <c>172.*</c> so the API — running on
/// any user-defined compose network — can reach the gateway. Without this,
/// IBeam authenticates internally but every /v1/api/* path returns 404 to
/// external callers, and WaitForAuthAsync hangs until timeout.
/// </summary>
public sealed class IBeamOptions
{
    public const string SectionName = "IBeam";

    /// <summary>
    /// Docker image reference for IBeam. Defaults to the local custom build
    /// (docker/Dockerfile.ibeam); see the type-level remarks for why the
    /// upstream <c>voyz/ibeam:latest</c> is not usable here directly.
    /// </summary>
    public string Image { get; set; } = "finance-sentry/ibeam:local";

    /// <summary>
    /// Compose network name to attach spawned containers to. Must match the
    /// network the API itself is on so it can resolve container names via DNS.
    /// </summary>
    public string Network { get; set; } = "docker_finance-sentry-network";

    /// <summary>
    /// Prefix for spawned container names. Full name is
    /// <c>{ContainerNamePrefix}-{shortId}</c> where shortId is the first 8 chars
    /// of the credential's Guid.
    /// </summary>
    public string ContainerNamePrefix { get; set; } = "finance-sentry-ibkr";

    /// <summary>
    /// Max seconds to wait for the gateway to auth after spawn. Must cover:
    /// IBeam cold-start (~90–120s for Java + Selenium), form submit + IBKR
    /// 2FA push round-trip (~5–20s), human tap-approve time (up to ~60s),
    /// and post-login DOM transition (~5s). 300s (5 min) gives comfortable
    /// headroom without being so long that a genuinely failed connect
    /// leaves the user waiting forever.
    /// </summary>
    public int SpawnTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// URI to reach the Docker daemon. Defaults to the unix socket bound into
    /// the API container.
    /// </summary>
    public string DockerEndpoint { get; set; } = "unix:///var/run/docker.sock";

    /// <summary>
    /// Option text IBeam clicks when IBKR shows the "select second factor
    /// device" screen (accounts with more than one 2FA method enrolled).
    /// Passed as IBEAM_TWO_FA_SELECT_TARGET; must match the option text on the
    /// login page exactly or IBeam stalls on the selector until timeout.
    /// IBeam's own default is "IB Key"; newer IBKR login pages label the
    /// mobile-app option "IBKR Mobile".
    /// </summary>
    public string TwoFaSelectTarget { get; set; } = "IB Key";
}
