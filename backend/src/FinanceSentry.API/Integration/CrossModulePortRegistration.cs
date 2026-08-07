namespace FinanceSentry.API.Integration;

using FinanceSentry.Modules.Research.Domain.Ports;
using FinanceSentry.Modules.Risk.Domain.Ports;

/// <summary>
/// 039: wires the cross-module read ports whose adapters live in the host. The Risk and Research
/// modules never reference each other; the composition root references both, so the adapters that
/// bridge them are registered here. See specs/039-ips-risk-boundary.
/// </summary>
public static class CrossModulePortRegistration
{
    public static IServiceCollection AddCrossModulePorts(this IServiceCollection services)
    {
        services.AddScoped<IAllocationPolicySource, IpsAllocationPolicySource>();
        services.AddScoped<IPositionCapSource, RiskPositionCapSource>();
        return services;
    }
}
