namespace FinanceSentry.Core.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public interface IModuleRegistrar
{
    void Register(IServiceCollection services, IConfiguration config);
}
