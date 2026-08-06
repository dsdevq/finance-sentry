namespace FinanceSentry.Modules.Analytics.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Analytics.API.Responses;
using FinanceSentry.Modules.Analytics.Application.Services;

/// <summary>Returns the curated queryable surface — the schema card (feature 033, US2, FR-007).</summary>
public sealed record DescribeQuerySchema : IQuery<QuerySchemaDto>;

public sealed class DescribeQuerySchemaHandler(ICuratedSchema schema)
    : IQueryHandler<DescribeQuerySchema, QuerySchemaDto>
{
    public Task<QuerySchemaDto> Handle(DescribeQuerySchema query, CancellationToken cancellationToken)
        => Task.FromResult(schema.Get());
}
