namespace FinanceSentry.Infrastructure.Fx;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ExchangeRateServiceCollectionExtensions
{
    private const string DefaultBaseUrl = "https://open.er-api.com/";
    private const int RequestTimeoutSeconds = 15;

    /// <summary>
    /// Registers the live FX rate provider + refresh job. Base URL is overridable
    /// via <c>ExchangeRates:BaseUrl</c> (defaults to open.er-api.com).
    /// </summary>
    public static IServiceCollection AddExchangeRates(
        this IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config["ExchangeRates:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = DefaultBaseUrl;

        services.AddHttpClient<IExchangeRateProvider, OpenErApiExchangeRateProvider>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
        });

        services.AddTransient<ExchangeRateRefreshJob>();

        return services;
    }
}
