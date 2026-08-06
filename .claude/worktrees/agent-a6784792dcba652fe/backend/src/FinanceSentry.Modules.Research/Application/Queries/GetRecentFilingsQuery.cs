namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Services;

public record GetRecentFilingsQuery(
    string Ticker,
    IReadOnlyList<string>? FormTypes,
    int Limit) : IQuery<IReadOnlyList<EdgarFilingDto>>;

public class GetRecentFilingsQueryHandler(ISecEdgarService svc)
    : IQueryHandler<GetRecentFilingsQuery, IReadOnlyList<EdgarFilingDto>>
{
    public async Task<IReadOnlyList<EdgarFilingDto>> Handle(GetRecentFilingsQuery query, CancellationToken ct)
    {
        var filings = await svc.GetRecentFilingsAsync(query.Ticker, query.FormTypes, query.Limit, ct);
        return filings
            .Select(f => new EdgarFilingDto(
                f.Ticker, f.Form, f.FilingDate, f.ReportDate, f.Description,
                f.AccessionNumber, f.DocumentUrl, f.IsXbrl))
            .ToList();
    }
}
