namespace FinanceSentry.Modules.BankSync.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;
using Microsoft.Extensions.Configuration;

public record BeginTrueLayerConnectRequest(string ProviderId, string? ProviderName);

public record BeginTrueLayerConnectCommand(
    Guid UserId,
    string ProviderId,
    string? ProviderName) : ICommand<BeginTrueLayerConnectResult>;

public record BeginTrueLayerConnectResult(string Link, string Reference);

public class BeginTrueLayerConnectCommandHandler(
    ITrueLayerClient client,
    ITrueLayerConnectionRepository connections,
    IConfiguration configuration)
    : ICommandHandler<BeginTrueLayerConnectCommand, BeginTrueLayerConnectResult>
{
    public async Task<BeginTrueLayerConnectResult> Handle(
        BeginTrueLayerConnectCommand request, CancellationToken cancellationToken)
    {
        var existing = await connections.GetByUserAndProviderAsync(
            request.UserId, request.ProviderId, cancellationToken);

        if (existing != null && existing.Status == "LINKED")
            throw new TrueLayerException(
                "TRUELAYER_PROVIDER_ALREADY_CONNECTED",
                "This bank is already connected. Disconnect it before reconnecting.",
                409);

        var callbackPath = configuration["TrueLayer:CallbackPath"]
            ?? "/api/v1/accounts/truelayer/callback";
        var publicApiBase = configuration["PublicApiBaseUrl"]
            ?? "http://localhost:5001";

        var reference = Guid.NewGuid().ToString("N");
        var redirectUri = $"{publicApiBase.TrimEnd('/')}{callbackPath}";

        var link = client.BuildAuthLink(request.ProviderId, reference, redirectUri);

        if (existing != null)
            await connections.DeleteAsync(existing.Id, cancellationToken);

        var entity = new TrueLayerConnection(
            userId: request.UserId,
            providerId: request.ProviderId,
            providerDisplayName: request.ProviderName ?? request.ProviderId,
            reference: reference);

        await connections.AddAsync(entity, cancellationToken);

        return new BeginTrueLayerConnectResult(Link: link, Reference: reference);
    }
}
