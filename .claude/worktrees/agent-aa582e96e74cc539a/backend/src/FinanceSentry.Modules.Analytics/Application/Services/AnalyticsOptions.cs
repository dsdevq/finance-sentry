namespace FinanceSentry.Modules.Analytics.Application.Services;

/// <summary>
/// Analytics query-tool tuning (feature 033). Bound from the <c>Analytics</c> config section;
/// <see cref="ReadOnlyConnectionString"/> is set from <c>ConnectionStrings:ReadOnly</c> during module
/// registration.
/// </summary>
public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>Connection string for the SELECT-only <c>fs_readonly</c> login. Set in module wiring.</summary>
    public string ReadOnlyConnectionString { get; set; } = string.Empty;

    /// <summary>Per-query statement timeout (ms) — the runaway-time guard (FR-006).</summary>
    public int StatementTimeoutMs { get; set; } = 5000;

    /// <summary>Maximum rows returned; results beyond this are clipped and flagged truncated (FR-006).</summary>
    public int MaxRows { get; set; } = 1000;
}
