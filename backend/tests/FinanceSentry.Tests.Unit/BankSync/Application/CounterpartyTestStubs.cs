namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// No-op stub: returns an empty classification result (no counterparty matches).
/// Used to keep existing statistics-service tests free of counterparty logic.
/// </summary>
internal sealed class NoOpCounterpartyClassificationService : ICounterpartyClassificationService
{
    public Task<CounterpartyClassificationResult> ClassifyAsync(
        Guid userId,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, string> accountCurrencies,
        CancellationToken ct = default)
        => Task.FromResult(new CounterpartyClassificationResult([], []));
}
