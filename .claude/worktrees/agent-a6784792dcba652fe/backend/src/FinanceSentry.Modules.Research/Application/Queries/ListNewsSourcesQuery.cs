namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>Lists all registered news sources with health fields (feature 030), enabled and disabled.</summary>
public record ListNewsSourcesQuery : IQuery<IReadOnlyList<NewsSourceDto>>;

public class ListNewsSourcesQueryHandler(INewsSourceRepository repo)
    : IQueryHandler<ListNewsSourcesQuery, IReadOnlyList<NewsSourceDto>>
{
    public async Task<IReadOnlyList<NewsSourceDto>> Handle(ListNewsSourcesQuery query, CancellationToken ct)
    {
        var sources = await repo.ListAllAsync(ct);
        return sources
            .Select(s => new NewsSourceDto(
                s.Id, s.Name, s.Kind.ToString(), s.Url, s.Keywords, s.ThesisId,
                s.Enabled, s.ConsecutiveFailures, s.LastSuccessAt, s.LastFailureReason))
            .ToList();
    }
}
