namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;

public record TrueLayerProviderDto(
    string ProviderId,
    string DisplayName,
    string Country,
    string? LogoUrl);

public record ListTrueLayerProvidersQuery(string? Country) : IQuery<IReadOnlyList<TrueLayerProviderDto>>;

public class ListTrueLayerProvidersQueryHandler(ITrueLayerClient client)
    : IQueryHandler<ListTrueLayerProvidersQuery, IReadOnlyList<TrueLayerProviderDto>>
{
    public async Task<IReadOnlyList<TrueLayerProviderDto>> Handle(
        ListTrueLayerProvidersQuery request, CancellationToken cancellationToken)
    {
        var country = string.IsNullOrWhiteSpace(request.Country)
            ? null
            : request.Country.Trim().ToLowerInvariant();

        var providers = await client.ListProvidersAsync(country, cancellationToken);
        return providers
            .Select(p => new TrueLayerProviderDto(
                ProviderId: p.ProviderId,
                DisplayName: p.DisplayName,
                Country: p.Country,
                LogoUrl: p.LogoUrl))
            .ToList();
    }
}
