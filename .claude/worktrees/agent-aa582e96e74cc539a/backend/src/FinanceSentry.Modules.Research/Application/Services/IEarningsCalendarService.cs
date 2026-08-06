namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IEarningsCalendarService
{
    // Fetches upcoming earnings + ex-dividend/dividend dates for the given tickers, live from
    // Yahoo Finance, filtered to [from, to] and (optionally) a single eventType.
    Task<IReadOnlyList<EarningsEvent>> GetForTickersAsync(
        IReadOnlyCollection<string> tickers,
        DateOnly from,
        DateOnly to,
        string? eventType,
        CancellationToken ct = default);
}
