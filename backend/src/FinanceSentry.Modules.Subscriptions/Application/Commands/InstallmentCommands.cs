namespace FinanceSentry.Modules.Subscriptions.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Exceptions;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

// --- Set / clear the installment schedule (total number of payments and/or final payment month) ---

public record SetInstallmentTermCommand(string UserId, Guid Id, int? TermCount, DateOnly? EndDate = null) : ICommand<bool>;

public class SetInstallmentTermCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<SetInstallmentTermCommand, bool>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<bool> Handle(SetInstallmentTermCommand command, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(command.Id, ct);
        if (item is null || item.UserId != command.UserId)
            throw new SubscriptionNotFoundException();

        item.SetTerm(command.TermCount, command.EndDate);
        await _repository.UpsertAsync(item, ct);
        return true;
    }
}

// --- Mark an installment finished ---

public record CompleteInstallmentCommand(string UserId, Guid Id) : ICommand<bool>;

public class CompleteInstallmentCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<CompleteInstallmentCommand, bool>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<bool> Handle(CompleteInstallmentCommand command, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(command.Id, ct);
        if (item is null || item.UserId != command.UserId)
            throw new SubscriptionNotFoundException();

        item.MarkCompleted();
        await _repository.UpsertAsync(item, ct);
        return true;
    }
}

// --- Delete an installment (primarily for manually-added ones) ---

public record DeleteInstallmentCommand(string UserId, Guid Id) : ICommand<bool>;

public class DeleteInstallmentCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<DeleteInstallmentCommand, bool>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<bool> Handle(DeleteInstallmentCommand command, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(command.Id, ct);
        if (item is null || item.UserId != command.UserId)
            throw new SubscriptionNotFoundException();

        await _repository.DeleteAsync(item, ct);
        return true;
    }
}

// --- Manually add an installment the detector missed ---

public record AddManualInstallmentCommand(
    string UserId,
    string Merchant,
    decimal MonthlyAmount,
    string Currency,
    DateOnly StartDate,
    int? TermCount) : ICommand<Guid>;

public class AddManualInstallmentCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<AddManualInstallmentCommand, Guid>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<Guid> Handle(AddManualInstallmentCommand command, CancellationToken ct)
    {
        var item = DetectedSubscription.CreateManual(
            command.UserId,
            command.Merchant.Trim(),
            command.MonthlyAmount,
            string.IsNullOrWhiteSpace(command.Currency) ? "USD" : command.Currency.Trim().ToUpperInvariant(),
            command.StartDate,
            command.TermCount is > 0 ? command.TermCount : null);

        await _repository.UpsertAsync(item, ct);
        return item.Id;
    }
}

// --- Manually add a subscription detection can't reliably find (e.g. an irregular Claude plan) ---

public record AddManualSubscriptionCommand(
    string UserId,
    string Merchant,
    decimal MonthlyAmount,
    string Currency,
    DateOnly StartDate) : ICommand<Guid>;

public class AddManualSubscriptionCommandHandler(IDetectedSubscriptionRepository repository)
    : ICommandHandler<AddManualSubscriptionCommand, Guid>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<Guid> Handle(AddManualSubscriptionCommand command, CancellationToken ct)
    {
        var item = DetectedSubscription.CreateManual(
            command.UserId,
            command.Merchant.Trim(),
            command.MonthlyAmount,
            string.IsNullOrWhiteSpace(command.Currency) ? "USD" : command.Currency.Trim().ToUpperInvariant(),
            command.StartDate,
            termCount: null,
            kind: SubscriptionKinds.Subscription);

        await _repository.UpsertAsync(item, ct);
        return item.Id;
    }
}
