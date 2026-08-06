namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface ISecEdgarService
{
    // Recent SEC filings for a ticker, newest first, optionally restricted to given form types
    // (e.g. 10-K, 10-Q, 8-K). Returns empty when the ticker maps to no US-listed EDGAR filer.
    Task<IReadOnlyList<EdgarFiling>> GetRecentFilingsAsync(
        string ticker,
        IReadOnlyCollection<string>? formTypes,
        int limit,
        CancellationToken ct = default);

    // Key reported fundamentals (revenue, gross profit, net income, diluted EPS, operating income,
    // shareholders' equity) from EDGAR XBRL, newest first, up to maxPerConcept datapoints each.
    Task<IReadOnlyList<FundamentalFact>> GetFundamentalsAsync(
        string ticker,
        int maxPerConcept,
        CancellationToken ct = default);
}
