namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Resolves the per-user IBeam gateway location. In stage 2 the container name
/// is derived from the IBKRCredential Id so both the container spawn and the
/// HTTP client resolve to the same address.
/// </summary>
public interface IIBeamGatewayResolver
{
    /// <summary>
    /// Returns the container name (used both for spawn and as DNS name on the
    /// shared Docker network) for the given credential id.
    /// </summary>
    string ContainerName(Guid credentialId);

    /// <summary>
    /// Returns the base URL the API uses to reach the user's IBeam gateway.
    /// </summary>
    Uri BaseUrl(Guid credentialId);
}
