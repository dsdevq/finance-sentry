namespace FinanceSentry.Modules.Subscriptions.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Exceptions;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public record RestoreSubscriptionCommand(string UserId, Guid SubscriptionId) : ICommand<bool>;

public class RestoreSubscriptionCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<RestoreSubscriptionCommand, bool>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<bool> Handle(RestoreSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(command.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.UserId != command.UserId || subscription.Status != SubscriptionStatus.Dismissed)
            throw new SubscriptionNotFoundException();

        subscription.Restore();
        await _repository.UpsertAsync(subscription, cancellationToken);
        return true;
    }
}
