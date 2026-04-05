namespace FinanceSentry.Modules.BankSync.Application.EventHandlers;

using FinanceSentry.Modules.BankSync.Domain.Events;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using MediatR;

/// <summary>
/// Handles <see cref="AccountSyncCompletedEvent"/> published after a sync job finishes.
/// Updates the BankAccount's SyncStatus to reflect the outcome.
/// </summary>
public class SyncCompletionEventHandler(IBankAccountRepository accounts) : INotificationHandler<AccountSyncCompletedEvent>
{
    private readonly IBankAccountRepository _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));

    /// <inheritdoc />
    public async Task Handle(AccountSyncCompletedEvent notification, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(notification.AccountId, cancellationToken);
        if (account == null)
            return; // account may have been deleted; nothing to update

        switch (notification.Status)
        {
            case "success":
                // Only transition from syncing → active (balance is already updated by ScheduledSyncService)
                if (account.SyncStatus == "syncing")
                    account.MarkActive(account.CurrentBalance ?? 0m);
                break;

            case "failed":
                if (account.SyncStatus == "syncing")
                    account.MarkFailed(notification.ErrorMessage);
                break;
        }

        await _accounts.UpdateAsync(account, cancellationToken);
    }
}
