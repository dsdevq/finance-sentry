namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Configuration for the per-user IBeam Docker orchestration.
///
/// The API container spawns one <c>voyz/ibeam</c> container per connected user,
/// joined to <see cref="Network"/> with a stable name so the API can address it
/// by DNS at <c>https://finance-sentry-ibkr-{shortId}:5000</c>. IBeam uses its
/// bundled default IBKR Client Portal Gateway conf; we no longer bind-mount a
/// custom conf.yaml because the extra vars we were pinning (ip2loc, allow-ip
/// ranges) interacted badly with the IB Key push 2FA flow.
/// </summary>
public sealed class IBeamOptions
{
    public const string SectionName = "IBeam";

    /// <summary>
    /// Docker image reference for IBeam. Defaults to <c>voyz/ibeam:latest</c>.
    /// </summary>
    public string Image { get; set; } = "voyz/ibeam:latest";

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
}
