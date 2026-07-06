namespace FinanceSentry.Modules.Research.Domain;

// A single SEC EDGAR filing. Fetched live from data.sec.gov; never persisted.
public sealed record EdgarFiling(
    string Ticker,
    string Form,
    DateOnly FilingDate,
    DateOnly? ReportDate,
    string Description,
    string AccessionNumber,
    string DocumentUrl,
    bool IsXbrl);
