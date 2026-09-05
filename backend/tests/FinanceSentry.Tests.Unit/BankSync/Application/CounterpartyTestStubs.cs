namespace FinanceSentry.Tests.Unit.BankSync.Application;

using FinanceSentry.Modules.BankSync.Application.Services;

/// <summary>
/// Classification results the statistics-service tests feed in directly. The readers take
/// the result as a parameter (it is computed once per request upstream), so the tests hand
/// over a value rather than stubbing a service.
/// </summary>
internal static class CounterpartyResults
{
    /// <summary>No counterparty matched anything — keeps a test free of counterparty logic.</summary>
    internal static CounterpartyClassificationResult None => new([], []);

    /// <summary>A result carrying the given monthly flows and no matched transaction ids.</summary>
    internal static CounterpartyClassificationResult WithFlows(params CounterpartyMonthlyFlow[] flows)
        => new([], flows);
}
