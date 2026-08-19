namespace FinanceSentry.Integration;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Services;
using FinanceSentry.Modules.Research.Domain.Ports;
using FinanceSentry.Modules.Risk.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Wires cross-module and cross-cutting service registrations whose consumers span more than one
/// module. Risk, Research, and Mcp all consume IBookFiguresService; registering it here rather
/// than in any single module prevents an undeclared coupling. See also 039: cross-module read ports.
/// </summary>
public static class CrossModulePortRegistration
{
    public static IServiceCollection AddCrossModulePorts(this IServiceCollection services)
    {
        services.AddScoped<IBookFiguresService, BookFiguresService>();

        services.AddScoped<IAllocationPolicySource, IpsAllocationPolicySource>();
        services.AddScoped<IPositionCapSource, RiskPositionCapSource>();
        return services;
    }
}
