namespace FinanceSentry.Modules.Analytics.Application.Services;

using FinanceSentry.Modules.Analytics.API.Responses;

/// <summary>
/// The authoritative catalog of curated views the query tool exposes (feature 033, US2). It mirrors the
/// views created in migration M001 — the only queryable surface — and is surfaced by
/// <c>describe_query_schema</c> and embedded in the run tool's description.
/// </summary>
public interface ICuratedSchema
{
    QuerySchemaDto Get();
}
