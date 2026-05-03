namespace FinanceSentry.Modules.Subscriptions.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Subscriptions.Domain.Exceptions;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public record DismissSubscriptionCommand(string UserId, Guid SubscriptionId) : ICommand<bool>;

public class DismissSubscriptionCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<DismissSubscriptionCommand, bool>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<bool> Handle(DismissSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(command.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.UserId != command.UserId)
            throw new SubscriptionNotFoundException();

        subscription.MarkDismissed();
        await _repository.UpsertAsync(subscription, cancellationToken);
        return true;
    }
}
