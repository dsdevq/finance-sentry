namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Configuration for the per-user IBeam Docker orchestration.
///
/// The API container spawns one <c>voyz/ibeam</c> container per connected user.
/// Each container joins <see cref="Network"/>, mounts the conf file from
/// <see cref="ConfHostPath"/>, and gets a stable name so the API can address it
/// by DNS at <c>https://finance-sentry-ibkr-{shortId}:5000</c>.
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
    /// Absolute path on the host to <c>docker/ibkr/conf.yaml</c>. The API bind
    /// mounts this into every spawned IBeam at <c>/srv/inputs/conf.yaml</c>.
    /// </summary>
    public string ConfHostPath { get; set; } = string.Empty;

    /// <summary>
    /// Prefix for spawned container names. Full name is
    /// <c>{ContainerNamePrefix}-{shortId}</c> where shortId is the first 8 chars
    /// of the credential's Guid.
    /// </summary>
    public string ContainerNamePrefix { get; set; } = "finance-sentry-ibkr";

    /// <summary>
    /// Max seconds to wait for the gateway to auth after spawn.
    /// </summary>
    public int SpawnTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// URI to reach the Docker daemon. Defaults to the unix socket bound into
    /// the API container.
    /// </summary>
    public string DockerEndpoint { get; set; } = "unix:///var/run/docker.sock";
}
