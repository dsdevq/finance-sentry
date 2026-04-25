namespace FinanceSentry.Modules.BankSync.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

/// <summary>
/// Links a bank account via Plaid public token exchange.
/// FR-001: securely stores encrypted access token.
/// FR-003: never logs plaintext tokens.
/// Publishes BankAccountConnectedEvent for async initial sync (T210).
/// </summary>
public record LinkRequest(string PublicToken, string InstitutionName);

public record ConnectBankAccountCommand(
    Guid UserId,
    string PublicToken,
    string InstitutionName
) : ICommand<ConnectBankAccountResult>;

public record ConnectBankAccountResult(
    Guid AccountId,
    string BankName,
    string AccountType,
    string AccountNumberLast4,
    string Currency,
    string SyncStatus);

public class ConnectBankAccountCommandHandler(
    PlaidAdapter plaid,
    ICredentialEncryptionService encryption,
    IBankAccountRepository accounts,
    IEncryptedCredentialRepository credentials,
    IEventBus events)
        : ICommandHandler<ConnectBankAccountCommand, ConnectBankAccountResult>
{
    public async Task<ConnectBankAccountResult> Handle(
          ConnectBankAccountCommand command, CancellationToken cancellationToken)
    {
        var exchange = await plaid.ExchangePublicTokenAsync(command.PublicToken, cancellationToken);

        var plaidAccounts = await plaid.GetAccountsWithBalanceAsync(exchange.AccessToken, cancellationToken);
        var primary = plaidAccounts.FirstOrDefault()
            ?? throw new InvalidOperationException("Plaid returned no accounts for this item.");

        var account = new BankAccount(
            userId: command.UserId,
            externalAccountId: exchange.ItemId,
            bankName: command.InstitutionName,
            accountType: primary.AccountType,
            accountNumberLast4: primary.AccountNumberLast4.Length >= 4
                ? primary.AccountNumberLast4[^4..]
                : primary.AccountNumberLast4.PadLeft(4, '0'),
            ownerName: string.Empty,
            currency: primary.Currency,
            createdBy: command.UserId);

        await accounts.AddAsync(account, cancellationToken);

        var encrypted = encryption.Encrypt(exchange.AccessToken);
        var credential = new EncryptedCredential(
            accountId: account.Id,
            encryptedData: encrypted.Ciphertext,
            iv: encrypted.Iv,
            authTag: encrypted.AuthTag,
            keyVersion: encrypted.KeyVersion);

        await credentials.AddAsync(credential, cancellationToken);
        await accounts.SaveChangesAsync(cancellationToken);

        await events.Publish(
            new BankAccountConnectedEvent(account.Id, command.UserId, account.Id),
            cancellationToken);

        return new ConnectBankAccountResult(
            account.Id, account.BankName, account.AccountType,
            account.AccountNumberLast4, account.Currency, account.SyncStatus);
    }
}

public record BankAccountConnectedEvent(
    Guid AccountId,
    Guid UserId,
    Guid CredentialId
) : IEvent;
