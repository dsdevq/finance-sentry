namespace FinanceSentry.Infrastructure.Observability.Hangfire;

using System.Net;
using System.Net.Sockets;
using global::Hangfire.Dashboard;

/// <summary>
/// Production Hangfire dashboard authorization (FR-004). Allows only requests arriving over loopback
/// (the Tailscale-serve reverse proxy terminates on localhost) or directly from the Tailscale CGNAT
/// range (100.64.0.0/10). Every other origin — including anything reaching the app publicly — is denied,
/// so the dashboard is never served to an unauthenticated public caller.
/// </summary>
public sealed class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // DashboardRequest.RemoteIpAddress avoids a Hangfire.AspNetCore dependency in this infra project.
        if (!IPAddress.TryParse(context.Request.RemoteIpAddress, out var remote))
            return false;

        return IPAddress.IsLoopback(remote) || IsTailscale(remote);
    }

    private static bool IsTailscale(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        // Tailscale assigns tailnet addresses from the 100.64.0.0/10 CGNAT block.
        var bytes = address.GetAddressBytes();
        return bytes[0] == 100 && (bytes[1] & 0xC0) == 0x40;
    }
}
