namespace FinanceSentry.Modules.Research.Domain;

// A single reported financial datapoint from SEC EDGAR XBRL company facts.
// Fetched live; never persisted. Value is in the given Unit (e.g. USD, USD/shares).
public sealed record FundamentalFact(
    string Ticker,
    string Concept,
    string Label,
    string Unit,
    decimal Value,
    DateOnly PeriodEnd,
    string? FiscalPeriod,
    int? FiscalYear,
    string Form);
