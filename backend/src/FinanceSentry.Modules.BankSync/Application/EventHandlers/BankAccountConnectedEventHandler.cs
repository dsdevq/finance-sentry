namespace FinanceSentry.Modules.BankSync.Application.EventHandlers;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Application.Commands;
using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

/// <summary>
/// Handles the initial transaction sync when a bank account is connected (T210).
/// Triggered by BankAccountConnectedEvent published in ConnectBankAccountCommandHandler.
///
/// Flow:
/// 1. Retrieve encrypted access token → decrypt → use for Plaid calls
/// 2. Create SyncJob (status=running)
/// 3. Fetch 12 months of transactions from Plaid
/// 4. Deduplicate against existing hashes
/// 5. Bulk insert new transactions
/// 6. Update BankAccount.SyncStatus → active
/// 7. Complete SyncJob (status=success or failed)
/// </summary>
public class BankAccountConnectedEventHandler(
    IBankAccountRepository accounts,
    ITransactionRepository transactions,
    ISyncJobRepository syncJobs,
    IEncryptedCredentialRepository credentials,
    ICredentialEncryptionService encryption,
    PlaidAdapter plaid,
    ITransactionDeduplicationService deduplication) : IEventHandler<BankAccountConnectedEvent>
{
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly ITransactionRepository _transactions = transactions;
    private readonly ISyncJobRepository _syncJobs = syncJobs;
    private readonly IEncryptedCredentialRepository _credentials = credentials;
    private readonly ICredentialEncryptionService _encryption = encryption;
    private readonly PlaidAdapter _plaid = plaid;
    private readonly ITransactionDeduplicationService _deduplication = deduplication;

    public async Task Handle(BankAccountConnectedEvent @event, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(@event.AccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Account {@event.AccountId} not found.");

        // Create sync job to track progress
        var syncJob = new SyncJob
        {
            AccountId = account.Id,
            Status = "running",
            StartedAt = DateTime.UtcNow
        };
        await _syncJobs.AddAsync(syncJob, cancellationToken);

        try
        {
            account.StartSync();
            await _accounts.UpdateAsync(account, cancellationToken);

            // Decrypt access token (FR-003: never logged)
            var credential = await _credentials.GetByAccountIdAsync(account.Id, cancellationToken)
                ?? throw new InvalidOperationException($"No credential found for account {account.Id}.");

            var accessToken = _encryption.Decrypt(
                credential.EncryptedData, credential.Iv, credential.AuthTag, credential.KeyVersion);
            credential.UpdateLastUsedAt();

            // Initial full sync — no cursor yet, Plaid returns all available history
            var (candidatesRaw, nextCursor) = await _plaid.SyncTransactionsAsync(
                accessToken, account.Id, @event.UserId, null, cancellationToken);
            var candidates = candidatesRaw.ToList();

            // Deduplicate: fetch existing hashes, filter candidates
            var existingHashes = (await _transactions.GetByAccountIdAsync(account.Id, cancellationToken))
                .Select(t => t.UniqueHash)
                .ToHashSet();

            var newCandidates = _deduplication.FilterDuplicates(candidates, existingHashes);

            // Bulk insert new transactions
            if (newCandidates.Count > 0)
            {
                var entities = newCandidates.Select(_deduplication.ToEntity).ToList();
                await _transactions.AddRangeAsync(entities, cancellationToken);
            }

            // Persist cursor so future syncs are incremental
            credential.PlaidSyncCursor = nextCursor;
            credential.UpdateLastUsedAt();
            await _credentials.UpdateAsync(credential, cancellationToken);

            // Mark account active with latest balance from Plaid
            var plaidAccounts = await _plaid.GetAccountsWithBalanceAsync(accessToken, cancellationToken);
            var latestBalance = plaidAccounts.FirstOrDefault()?.CurrentBalance ?? 0m;
            account.MarkActive(latestBalance);
            await _accounts.UpdateAsync(account, cancellationToken);

            // Complete sync job
            syncJob.Status = "success";
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.TransactionsSynced = newCandidates.Count;
        }
        catch (Exception ex)
        {
            account.MarkFailed(ex.Message[..Math.Min(ex.Message.Length, 100)]);
            await _accounts.UpdateAsync(account, cancellationToken);

            syncJob.Status = "failed";
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)];
        }
        finally
        {
            await _syncJobs.UpdateAsync(syncJob, cancellationToken);
            await _accounts.SaveChangesAsync(cancellationToken);
        }
    }
}
