namespace FinanceSentry.Modules.Analytics.API.Responses;

/// <summary>One column of a curated view — name + Postgres type (feature 033, US2).</summary>
public sealed record QueryColumnDto(string Name, string Type);

/// <summary>A curated view the agent may query, with its columns and a one-line purpose.</summary>
public sealed record QueryViewDto(string Name, string Purpose, IReadOnlyList<QueryColumnDto> Columns);

/// <summary>The queryable surface — only the curated views, never raw internal tables (FR-007, SC-005).</summary>
public sealed record QuerySchemaDto(IReadOnlyList<QueryViewDto> Views);
