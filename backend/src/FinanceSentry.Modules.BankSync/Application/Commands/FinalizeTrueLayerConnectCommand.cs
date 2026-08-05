namespace FinanceSentry.Modules.BankSync.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.TrueLayer;
using Hangfire;
using Microsoft.Extensions.Configuration;

public record FinalizeTrueLayerConnectCommand(string Reference, string Code) : ICommand<FinalizeTrueLayerConnectResult>;

public record FinalizeTrueLayerConnectResult(
    Guid UserId,
    int AccountsConnected,
    IReadOnlyList<Guid> CreatedAccountIds);

public class FinalizeTrueLayerConnectCommandHandler(
    ITrueLayerClient client,
    ITrueLayerConnectionRepository connections,
    ICredentialEncryptionService encryption,
    IBankAccountRepository accounts,
    IBackgroundJobClient backgroundJobs,
    IConfiguration configuration)
    : ICommandHandler<FinalizeTrueLayerConnectCommand, FinalizeTrueLayerConnectResult>
{
    public async Task<FinalizeTrueLayerConnectResult> Handle(
        FinalizeTrueLayerConnectCommand request, CancellationToken cancellationToken)
    {
        var connection = await connections.GetByReferenceAsync(request.Reference, cancellationToken)
            ?? throw new TrueLayerException(
                "TRUELAYER_CONNECTION_NOT_FOUND",
                $"No TrueLayer connection found for state '{request.Reference}'.",
                404);

        if (connection.Status == "LINKED")
            return new FinalizeTrueLayerConnectResult(connection.UserId, connection.BankAccounts.Count, []);

        var callbackPath = configuration["TrueLayer:CallbackPath"]
            ?? "/api/v1/accounts/truelayer/callback";
        var publicApiBase = configuration["PublicApiBaseUrl"]
            ?? "http://localhost:5001";
        var redirectUri = $"{publicApiBase.TrimEnd('/')}{callbackPath}";

        var tokens = await client.ExchangeCodeAsync(request.Code, redirectUri, cancellationToken);
        if (string.IsNullOrEmpty(tokens.RefreshToken))
            throw new TrueLayerException(
                "TRUELAYER_NO_REFRESH_TOKEN",
                "Bank returned no refresh token. Ensure offline_access scope is requested.",
                400);

        var encrypted = encryption.Encrypt(tokens.RefreshToken);
        connection.SetRefreshToken(encrypted.Ciphertext, encrypted.Iv, encrypted.AuthTag);
        connection.MarkLinked(expiresAt: DateTime.UtcNow.AddDays(90));
        await connections.UpdateAsync(connection, cancellationToken);

        var providerAccounts = await client.ListAccountsAsync(tokens.AccessToken, cancellationToken);
        var createdIds = new List<Guid>();

        foreach (var pa in providerAccounts)
        {
            decimal? currentBalance = null;
            try
            {
                var bal = await client.GetBalanceAsync(tokens.AccessToken, pa.AccountId, cancellationToken);
                currentBalance = bal?.Current;
            }
            catch (TrueLayerException)
            {
                // Best-effort: skip balance, account is still usable.
            }

            var existing = await accounts.GetByPlaidItemIdAsync(pa.AccountId, cancellationToken);
            if (existing != null)
            {
                // Reconnect/reauth: heal the existing account in place instead of skipping it.
                // Re-point it at the freshly linked connection and clear reauth_required so the
                // scheduler resumes syncing. A follow-up sync is enqueued below to backfill data.
                existing.MarkReconnected(connection.Id, currentBalance ?? existing.CurrentBalance ?? 0m);
                await accounts.UpdateAsync(existing, cancellationToken);
                createdIds.Add(existing.Id);
                continue;
            }

            var account = new BankAccount(
                userId: connection.UserId,
                externalAccountId: pa.AccountId,
                bankName: !string.IsNullOrWhiteSpace(pa.ProviderName) ? pa.ProviderName : connection.ProviderDisplayName,
                accountType: pa.AccountType,
                accountNumberLast4: pa.AccountNumberLast4,
                ownerName: string.Empty,
                currency: pa.Currency,
                createdBy: connection.UserId,
                provider: "truelayer")
            {
                TrueLayerConnectionId = connection.Id,
                CurrentBalance = currentBalance
            };

            await accounts.AddAsync(account, cancellationToken);
            createdIds.Add(account.Id);
        }

        foreach (var accountId in createdIds)
            backgroundJobs.Enqueue<FinanceSentry.Modules.BankSync.Infrastructure.Jobs.ScheduledSyncJob>(
                job => job.ExecuteSyncAsync(accountId));

        return new FinalizeTrueLayerConnectResult(
            UserId: connection.UserId,
            AccountsConnected: createdIds.Count,
            CreatedAccountIds: createdIds);
    }
}
