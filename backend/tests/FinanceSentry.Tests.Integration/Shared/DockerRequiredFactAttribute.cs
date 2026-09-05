namespace FinanceSentry.Tests.Integration.Shared;

using Xunit;

/// <summary>
/// A <see cref="FactAttribute"/> that reports the test as skipped when no Docker daemon is
/// reachable, instead of letting Testcontainers throw and fail the run.
/// <para>
/// CI runs the whole solution with no category filter (509), so <c>[Trait("Category","Integration")]</c>
/// alone does not keep a container-backed test out of the gate. The skip is decided here, at
/// discovery time, because <c>xunit.runner.visualstudio</c> 2.5.4 surfaces a runtime
/// <c>SkipException</c> as <em>Failed</em>.
/// </para>
/// </summary>
public sealed class DockerRequiredFactAttribute : FactAttribute
{
    private const string UnixScheme = "unix://";

    private const string NoDockerReason =
        "Docker is not available on this host — container-backed test skipped. "
        + "It runs for real wherever a Docker daemon is reachable (including CI).";

    private static readonly Lazy<bool> DockerAvailable = new(ProbeDocker);

    public DockerRequiredFactAttribute()
    {
        if (!DockerAvailable.Value)
        {
            Skip = NoDockerReason;
        }
    }

    private static bool ProbeDocker()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            // TCP and npipe endpoints cannot be probed from the file system — trust the operator
            // rather than skipping a test they explicitly pointed at a daemon.
            return !dockerHost.StartsWith(UnixScheme, StringComparison.OrdinalIgnoreCase)
                || File.Exists(dockerHost[UnixScheme.Length..]);
        }

        var runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string?[] candidates =
        [
            "/var/run/docker.sock",
            string.IsNullOrWhiteSpace(runtimeDir) ? null : Path.Combine(runtimeDir, "docker.sock"),
            // Docker Desktop (macOS) and rootless Docker both expose a per-user socket.
            string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, ".docker", "run", "docker.sock"),
        ];

        return candidates.Any(path => path is not null && File.Exists(path));
    }
}
