using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Spawns per-user IBeam containers via the Docker daemon (mounted docker.sock).
/// Every connected user gets one container named after their credential id so
/// the API can address them deterministically by DNS on the shared Docker
/// network.
/// </summary>
public sealed class IBeamContainerManager : IIBeamContainerManager
{
    private const int GatewayContainerPort = 5000;
    private const int AuthPollIntervalMs = 3000;

    private readonly IDockerClient _docker;
    private readonly IBeamOptions _options;
    private readonly IIBeamGatewayResolver _resolver;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IBeamContainerManager> _logger;

    public IBeamContainerManager(
        IDockerClient docker,
        IOptions<IBeamOptions> options,
        IIBeamGatewayResolver resolver,
        IHttpClientFactory httpClientFactory,
        ILogger<IBeamContainerManager> logger)
    {
        _docker = docker;
        _options = options.Value;
        _resolver = resolver;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SpawnAsync(Guid credentialId, string ibkrUsername, string ibkrPassword, CancellationToken ct = default)
    {
        var containerName = _resolver.ContainerName(credentialId);

        // Wipe any pre-existing container of the same name so spawn is idempotent.
        await RemoveIfExistsAsync(containerName, ct);

        await EnsureImageAsync(_options.Image, ct);

        var createResp = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = containerName,
            Image = _options.Image,
            // Matches the env-var shape that empirically worked on the old
            // single-tenant `ibkr-gateway` service (commit 233224a). The
            // aggressive limits I added in #245 (IBEAM_MAX_FAILED_AUTH=1,
            // IBEAM_REQUEST_RETRIES=1) turned out to fight IB Key push 2FA:
            // IBeam counts "browser login DOM ok but /auth/status still false"
            // as a failed attempt, and with =1 the container dies before the
            // user can tap approve on their phone. Reverting to IBeam's own
            // defaults (5 attempts) so the polling loop overlaps the human
            // tap window. The PAGE_LOAD_TIMEOUT bump we keep — it's a pure
            // widening of the Selenium DOM-wait, no downside.
            Env =
            [
                $"IBEAM_ACCOUNT={ibkrUsername}",
                $"IBEAM_PASSWORD={ibkrPassword}",
                "IBEAM_GATEWAY_BASE_URL=https://localhost:5000",
                "IBEAM_LOG_LEVEL=INFO",
                "IBEAM_ERROR_SCREENSHOTS=True",
                "IBEAM_PAGE_LOAD_TIMEOUT=180",
            ],
            HostConfig = new HostConfig
            {
                // No conf.yaml bind-mount. The old single-tenant service that
                // worked with 2FA didn't mount one either, and the custom conf
                // we were pinning (ip2loc, allow-ip ranges) is the most likely
                // reason IBeam's CPG reported authenticated=false after login.
                NetworkMode = _options.Network,
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped },
            },
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [_options.Network] = new EndpointSettings
                    {
                        Aliases = [containerName],
                    },
                },
            },
        }, ct);

        await _docker.Containers.StartContainerAsync(createResp.ID, new ContainerStartParameters(), ct);
        _logger.LogInformation(
            "Spawned IBeam container {Container} (id {Id}) for credential {CredentialId}",
            containerName, createResp.ID, credentialId);
    }

    public async Task StopAndRemoveAsync(Guid credentialId, CancellationToken ct = default)
    {
        var containerName = _resolver.ContainerName(credentialId);
        await RemoveIfExistsAsync(containerName, ct);
    }

    public async Task<bool> IsRunningAsync(Guid credentialId, CancellationToken ct = default)
    {
        var containerName = _resolver.ContainerName(credentialId);
        var containers = await _docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [containerName] = true },
            },
        }, ct);

        return containers.Any(c => string.Equals(c.State, "running", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> WaitForAuthAsync(Guid credentialId, CancellationToken ct = default)
    {
        var baseUrl = _resolver.BaseUrl(credentialId);
        var deadline = DateTime.UtcNow.AddSeconds(_options.SpawnTimeoutSeconds);

        using var http = CreateInsecureClient();

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await http.GetAsync(new Uri(baseUrl, "/v1/api/iserver/auth/status"), ct);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    if (body.Contains("\"authenticated\":true", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("IBeam container for credential {CredentialId} authenticated", credentialId);
                        return true;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Container may still be starting; retry.
            }

            await Task.Delay(AuthPollIntervalMs, ct);
        }

        _logger.LogWarning(
            "IBeam container for credential {CredentialId} did not authenticate within {Timeout}s",
            credentialId, _options.SpawnTimeoutSeconds);
        return false;
    }

    private async Task RemoveIfExistsAsync(string containerName, CancellationToken ct)
    {
        var existing = await _docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [containerName] = true },
            },
        }, ct);

        foreach (var container in existing)
        {
            _logger.LogInformation("Removing existing IBeam container {Container} (id {Id})", containerName, container.ID);
            try
            {
                await _docker.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters
                {
                    Force = true,
                    RemoveVolumes = true,
                }, ct);
            }
            catch (DockerContainerNotFoundException)
            {
                // Raced with another remove; safe to ignore.
            }
        }
    }

    private async Task EnsureImageAsync(string image, CancellationToken ct)
    {
        var images = await _docker.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["reference"] = new Dictionary<string, bool> { [image] = true },
            },
        }, ct);

        if (images.Count > 0)
            return;

        _logger.LogInformation("Pulling IBeam image {Image}", image);
        await _docker.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = image },
            authConfig: null,
            new Progress<JSONMessage>(),
            ct);
    }

    private HttpClient CreateInsecureClient()
    {
        // IBeam serves a self-signed cert. We only speak to the container over
        // the private Docker network so validating identity provides no extra
        // security here — trust is anchored on the network topology.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }
}
