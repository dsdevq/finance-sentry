namespace FinanceSentry.Modules.Analytics.API.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Unified response for <c>run_analytics_query</c> (feature 033). On success carries the result grid;
/// on a guard rejection or a time/row-budget overrun carries <see cref="Error"/> + <see cref="Reason"/>.
/// <see cref="Sql"/> is ALWAYS echoed (FR-001) so the agent can cite it and Denys can audit it.
/// Null members are omitted from the JSON so a success payload has no error fields and vice versa.
/// </summary>
public sealed record AnalyticsQueryResponse
{
    /// <summary>The submitted SQL, always echoed regardless of outcome.</summary>
    public required string Sql { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Columns { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<IReadOnlyList<object?>>? Rows { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RowCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Truncated { get; init; }

    /// <summary><c>rejected</c> | <c>too_large</c> when the query did not return a grid.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; init; }

    public static AnalyticsQueryResponse Success(
        string sql, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows, bool truncated)
        => new()
        {
            Sql = sql,
            Columns = columns,
            Rows = rows,
            RowCount = rows.Count,
            Truncated = truncated,
        };

    public static AnalyticsQueryResponse Rejected(string sql, string reason)
        => new() { Sql = sql, Error = "rejected", Reason = reason };

    public static AnalyticsQueryResponse TooLarge(string sql, string reason)
        => new() { Sql = sql, Error = "too_large", Reason = reason };
}
