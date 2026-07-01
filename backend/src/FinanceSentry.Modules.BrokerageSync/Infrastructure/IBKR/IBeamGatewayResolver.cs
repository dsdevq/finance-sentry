using Microsoft.Extensions.Options;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed class IBeamGatewayResolver(IOptions<IBeamOptions> options) : IIBeamGatewayResolver
{
    private const int ShortIdLength = 8;
    private const int GatewayPort = 5000;

    private readonly IBeamOptions _options = options.Value;

    public string ContainerName(Guid credentialId)
    {
        var shortId = credentialId.ToString("N")[..ShortIdLength];
        return $"{_options.ContainerNamePrefix}-{shortId}";
    }

    public Uri BaseUrl(Guid credentialId)
        => new($"https://{ContainerName(credentialId)}:{GatewayPort}");
}
