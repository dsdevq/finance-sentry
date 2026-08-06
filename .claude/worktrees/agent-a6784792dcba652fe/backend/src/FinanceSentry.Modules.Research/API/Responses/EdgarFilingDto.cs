namespace FinanceSentry.Modules.Research.API.Responses;

public record EdgarFilingDto(
    string Ticker,
    string Form,
    DateOnly FilingDate,
    DateOnly? ReportDate,
    string Description,
    string AccessionNumber,
    string DocumentUrl,
    bool IsXbrl);
