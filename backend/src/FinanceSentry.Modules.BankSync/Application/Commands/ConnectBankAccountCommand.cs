namespace FinanceSentry.Modules.BankSync.Application.Commands;

using FinanceSentry.Infrastructure.Encryption;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Modules.BankSync.Infrastructure.Plaid;
using MediatR;

// ──────────────────────────────────────────────
// Command
// ──────────────────────────────────────────────

/// <summary>
/// Links a bank account via Plaid public token exchange.
/// FR-001: securely stores encrypted access token.
/// FR-003: never logs plaintext tokens.
/// Publishes BankAccountConnectedEvent for async initial sync (T210).
/// </summary>
public record ConnectBankAccountCommand(
    Guid UserId,
    string PublicToken,
    string InstitutionName
) : IRequest<ConnectBankAccountResult>;

public record ConnectBankAccountResult(
    Guid AccountId,
    string BankName,
    string AccountType,
    string AccountNumberLast4,
    string Currency,
    string SyncStatus);

// ──────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────

public class ConnectBankAccountCommandHandler(
    PlaidAdapter plaid,
    ICredentialEncryptionService encryption,
    IBankAccountRepository accounts,
    IEncryptedCredentialRepository credentials,
    IMediator mediator)
        : IRequestHandler<ConnectBankAccountCommand, ConnectBankAccountResult>
{
    private readonly PlaidAdapter _plaid = plaid;
    private readonly ICredentialEncryptionService _encryption = encryption;
    private readonly IBankAccountRepository _accounts = accounts;
    private readonly IEncryptedCredentialRepository _credentials = credentials;
    private readonly IMediator _mediator = mediator;

    public async Task<ConnectBankAccountResult> Handle(
          ConnectBankAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Exchange public token → access token (never logged)
        var exchange = await _plaid.ExchangePublicTokenAsync(request.PublicToken, cancellationToken);

        // 2. Fetch accounts + balances from Plaid
        var plaidAccounts = await _plaid.GetAccountsWithBalanceAsync(exchange.AccessToken, cancellationToken);
        var primary = plaidAccounts.FirstOrDefault()
            ?? throw new InvalidOperationException("Plaid returned no accounts for this item.");

        // 3. Create domain entity — starts in 'pending' state
        var account = new BankAccount(
            userId: request.UserId,
            plaidItemId: exchange.ItemId,
            bankName: request.InstitutionName,
            accountType: primary.AccountType,
            accountNumberLast4: primary.AccountNumberLast4.Length >= 4
                ? primary.AccountNumberLast4[^4..]
                : primary.AccountNumberLast4.PadLeft(4, '0'),
            ownerName: string.Empty,
            currency: primary.Currency,
            createdBy: request.UserId);

        await _accounts.AddAsync(account, cancellationToken);

        // 4. Encrypt access token and store — FR-001, FR-003
        var encrypted = _encryption.Encrypt(exchange.AccessToken);
        var credential = new EncryptedCredential(
            accountId: account.Id,
            encryptedData: encrypted.Ciphertext,
            iv: encrypted.Iv,
            authTag: encrypted.AuthTag,
            keyVersion: encrypted.KeyVersion);

        await _credentials.AddAsync(credential, cancellationToken);
        await _accounts.SaveChangesAsync(cancellationToken);

        // 5. Publish event for async initial sync (T210 handler)
        await _mediator.Publish(
            new BankAccountConnectedEvent(account.Id, request.UserId, account.Id),
            cancellationToken);

        return new ConnectBankAccountResult(
            account.Id, account.BankName, account.AccountType,
            account.AccountNumberLast4, account.Currency, account.SyncStatus);
    }
}

// ──────────────────────────────────────────────
// Domain Event
// ──────────────────────────────────────────────

public record BankAccountConnectedEvent(
    Guid AccountId,
    Guid UserId,
    Guid CredentialId
) : INotification;
